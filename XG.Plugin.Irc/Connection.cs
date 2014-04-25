// 
//  Connection.cs
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
using Quartz;
using XG.Plugin.Job;

namespace XG.Plugin.Irc
{
	public abstract class Connection : AWorker
	{
		JobKey _jobKey;

		public DateTime LastContact { get; protected set; }

		protected abstract void RepairConnection();

		public void StartWatch(Int64 aWatchSeconds, string aName)
		{
			_jobKey = new JobKey(aName, "Connection");

			var data = new JobDataMap();
			data.Add("Connection", this);
			data.Add("MaximalTimeAfterLastContact", aWatchSeconds);

			IJobDetail job = JobBuilder.Create<ConnectionWatcher>()
				.WithIdentity(_jobKey)
				.UsingJobData(data)
				.Build();

			ITrigger trigger = TriggerBuilder.Create()
				.WithIdentity(aName, "Connection")
				.WithSimpleSchedule(x => x.WithIntervalInSeconds(1).RepeatForever())
				.Build();

			Scheduler.ScheduleJob(job, trigger);
		}

		public void Stopwatch()
		{
			Scheduler.DeleteJob(_jobKey);
		}
	}
}
