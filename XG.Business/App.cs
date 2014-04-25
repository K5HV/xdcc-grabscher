// 
//  App.cs
//  This file is part of XG - XDCC Grabscher
//  http://www.larsformella.de/lang/en/portfolio/programme-software/xg
//
//  Author:
//       Lars Formella <ich@larsformella.de>
// 
//  Copyright (c) 2012 Lars Formella
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
//  

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using log4net;
using SharpRobin.Core;
using XG.Business.Helper;
using XG.Model.Domain;
using XG.Plugin;
using XG.Config.Properties;
using XG.Business.Job;
using XG.DB;
using Quartz;
using Quartz.Impl;

namespace XG.Business
{
	public class App : APlugin
	{
		#region VARIABLES

		static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		readonly Dao _dao;

		readonly Plugins _plugins;

		readonly IScheduler _scheduler;

		RrdDb _rrdDb;

		public RrdDb RrdDb
		{
			get
			{
				return _rrdDb;
			}
		}

		public bool ShutdownInProgress
		{
			get;
			private set;
		}

		#endregion

		#region EVENTS

		public virtual event EmptyEventHandler OnShutdownComplete;

		protected void FireShutdownComplete()
		{
			if (OnShutdownComplete != null)
			{
				OnShutdownComplete();
			}
		}

		#endregion

		#region FUNCTIONS

		public App()
		{
			_scheduler = new StdSchedulerFactory().GetScheduler();
			_scheduler.Start();

			_dao = new Dao(_scheduler);

			FileActions.OnNotificationAdded += NotificationAdded;

			_plugins = new Plugins();
			_rrdDb = new Helper.Rrd().GetDb();

			LoadObjects();
			CheckForDuplicates();
			ResetObjects();
			ClearOldDownloads();
			TryToRecoverOpenFiles();
		}

		void TryToRecoverOpenFiles()
		{
			foreach (XG.Model.Domain.File file in Files.All)
			{
				var info = new FileInfo(Settings.Default.TempPath + file.TmpName);

				// lets check if the file is still on the harddisk
				if (!info.Exists)
				{
					Log.Warn("TryToRecoverOpenFiles() " + info.FullName + " is missing ");
					Files.Remove(file);
					continue;
				}
				else if (!file.Enabled)
				{
					// check if the real file and the part is actual the same
					if (file.CurrentSize != info.Length)
					{
						Log.Warn("TryToRecoverOpenFiles() size mismatch of " + file + " - db:" + file.CurrentSize + " real:" + info.Length);
						file.CurrentSize = info.Length;
					}

					// uhh, this is bad - close it and hope it works again
					if (file.Connected)
					{
						file.Connected = false;
					}

					file.Commit();
					if (!file.Enabled && file.MissingSize == 0)
					{
						FileActions.FinishFile(file);
					}
				}
			}
		}

		void StartWorkers()
		{
			var data1 = new JobDataMap();
			data1.Add("RrdDB", _rrdDb);
			AddJob(typeof(Job.Rrd), data1, Settings.Default.TakeSnapshotTimeInMinutes * 60);
			
			var data2 = new JobDataMap();
			data2.Add("Servers", Servers);
			AddJob(typeof(BotWatchdog), data2, Settings.Default.BotOfflineCheckTime);

			_plugins.StartAll();
		}

		void AddJob(Type aType, JobDataMap aData, int aInterval)
		{
			IJobDetail job = JobBuilder.Create(aType)
				.WithIdentity(aType.Name, "Core")
				.UsingJobData(aData)
				.Build();

			ITrigger trigger = TriggerBuilder.Create()
				.WithIdentity(aType.Name, "Core")
				.WithSimpleSchedule(x => x.WithIntervalInSeconds(aInterval).RepeatForever())
				.Build();

			_scheduler.ScheduleJob(job, trigger);
		}

		void ClearOldDownloads()
		{
			List<string> files = Directory.GetFiles(Settings.Default.TempPath).ToList();

			foreach (XG.Model.Domain.File file in Files.All)
			{
				if (file.Enabled)
				{
					Files.Remove(file);
					Log.Info("Run() removing ready " + file);
				}
				else
				{
					string path = Settings.Default.TempPath + file.TmpName;
					files.Remove(path);
				}
			}

			foreach (string dir in files)
			{
				FileSystem.DeleteFile(dir);
			}
		}

		void ResetObjects()
		{
			foreach (Server tServer in Servers.All)
			{
				tServer.Connected = false;
				tServer.ErrorCode = SocketErrorCode.None;

				foreach (Channel tChannel in tServer.Channels)
				{
					tChannel.Connected = false;
					tChannel.ErrorCode = 0;

					foreach (Bot tBot in tChannel.Bots)
					{
						tBot.Connected = false;
						tBot.State = Bot.States.Idle;
						tBot.QueuePosition = 0;
						tBot.QueueTime = 0;

						foreach (Packet pack in tBot.Packets)
						{
							pack.Connected = false;
						}
					}
				}
			}
		}

		void CheckForDuplicates()
		{
			foreach (Server serv in Servers.All)
			{
				foreach (Server s in Servers.All)
				{
					if (s.Name == serv.Name && s.Guid != serv.Guid)
					{
						Log.Error("Run() removing dupe " + s);
						Servers.Remove(s);
					}
				}

				foreach (Channel chan in serv.Channels)
				{
					foreach (Channel c in serv.Channels)
					{
						if (c.Name == chan.Name && c.Guid != chan.Guid)
						{
							Log.Error("Run() removing dupe " + c);
							serv.RemoveChannel(c);
						}
					}

					foreach (Bot bot in chan.Bots)
					{
						foreach (Bot b in chan.Bots)
						{
							if (b.Name == bot.Name && b.Guid != bot.Guid)
							{
								Log.Error("Run() removing dupe " + b);
								chan.RemoveBot(b);
							}
						}

						foreach (Packet pack in bot.Packets)
						{
							foreach (Packet p in bot.Packets)
							{
								if (p.Id == pack.Id && p.Guid != pack.Guid)
								{
									Log.Error("Run() removing dupe " + p);
									bot.RemovePacket(p);
								}
							}
						}
					}
				}
			}
		}

		void LoadObjects()
		{
			Servers = _dao.Servers;
			Files = _dao.Files;
			Searches = _dao.Searches;
			ApiKeys = _dao.ApiKeys;

			FileActions.Files = Files;
			FileActions.Servers = Servers;
			Notifications = new Notifications();
			Snapshots.Servers = Servers;
			Snapshots.Files = Files;
		}

		public void Shutdown(object sender)
		{
			if (!ShutdownInProgress)
			{
				ShutdownInProgress = true;
				Log.Warn("OnShutdown() triggered by " + sender);
				Stop();
				FireShutdownComplete();
			}
		}

		#endregion

		#region AWorker

		protected override void StartRun()
		{
			StartWorkers();
		}

		protected override void StopRun()
		{
			_scheduler.Shutdown();
			_dao.Dispose();
			_plugins.StopAll();
		}

		public void AddPlugin(APlugin aPlugin)
		{
			aPlugin.Servers = Servers;
			aPlugin.Files = Files;
			aPlugin.Searches = Searches;
			aPlugin.Notifications = Notifications;
			aPlugin.ApiKeys = ApiKeys;
			aPlugin.Scheduler = _scheduler;

			_plugins.Add(aPlugin);
		}

		#endregion
	}
}
