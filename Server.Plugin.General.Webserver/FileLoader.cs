// 
//  FileLoader.cs
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
using System.Reflection;

using log4net;

namespace XG.Server.Plugin.General.Webserver
{
	public class FileLoader
	{
		#region VARIABLES

		static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		Dictionary<string, string> _dicString;
		Dictionary<string, byte[]> _dicByte;

		string[] _cssFiles =
		{
			"reset",
			"fontello",
			"jquery-ui",
			"ui.jqgrid",
			"layout-default",
			"xg"
		};

		string[] _jsFiles =
		{
			"sha256",
			"json2.min",
			"jquery.min",
			"jquery-ui.min",
			"jquery.class",
			"jquery.cookie.min",
			"jquery.layout.min",
			"jquery.flot",
			"jquery.jqGrid.min",
			"i18n/grid.locale-#LANGUAGE_SHORT#",
			"i18n/xg.locale-en",
			"xg.enum",
			"xg.cookie",
			"xg.helper",
			"xg.formatter",
			"xg.resize",
			"xg.refresh",
			"xg.password",
			"xg.url",
			"xg",
			"i18n/xg.locale-#LANGUAGE_SHORT#"
		};

		#endregion

		public static FileLoader Instance
		{
			get { return Nested.Instance; }
		}
		class Nested
		{
			static Nested() { }
			internal static readonly FileLoader Instance = new FileLoader();
		}

		FileLoader()
		{
			_dicString = new Dictionary<string, string>();
			_dicByte = new Dictionary<string, byte[]>();
		}

		public string LoadFile(string aFile, string[] aLanguages)
		{
			if (_dicString.ContainsKey(aFile))
			{
				return _dicString[aFile];
			}
			else
			{
#if !UNSAFE
				try
				{
#endif
					string content = "";
					if (aFile == "/js/all.js")
					{
						foreach(string file in _jsFiles)
						{
							content += LoadFile("/js/" + file + ".js", aLanguages) + "\n";
						}
					}
					else if (aFile == "/css/all.css")
					{
						foreach(string file in _cssFiles)
						{
							content += LoadFile("/css/" + file + ".css", aLanguages) + "\n";
						}
					}
					else
					{
#if DEBUG
						content = File.OpenText("./Resources" + aFile).ReadToEnd();
#else
						Assembly assembly = Assembly.GetAssembly(typeof(FileLoader));
						string name = "XG." + assembly.GetName().Name + ".Resources" + aFile.Replace('/', '.');
						content = new StreamReader(assembly.GetManifestResourceStream(name)).ReadToEnd();
#endif
						if (aFile == "/index.html")
						{
#if DEBUG
							string css = "";
							foreach (string cssFile in _cssFiles)
							{
								css += "\t\t<link rel=\"stylesheet\" type=\"text/css\" media=\"screen\" href=\"css/" + cssFile + ".css\" />\n";
							}
#else
							string css = "\t\t<link rel=\"stylesheet\" type=\"text/css\" media=\"screen\" href=\"css/all.css\" />\n";
#endif
							content = content.Replace("#CSS_FILES#", css);
							
//#if DEBUG
							string js = "";
							foreach (string jsFile in _jsFiles)
							{
								js += "\t\t<script type=\"text/javascript\" src=\"js/" + jsFile + ".js\"></script>\n";
							}
//#else
//							string js = "\t\t<script type=\"text/javascript\" src=\"js/all.js\"></script>\n";
//#endif 
							content = content.Replace("#JS_FILES#", js);
						}

						content = PatchLanguage(content, aLanguages);
					}
#if !DEBUG
					_dicString.Add(aFile, content);
#endif
#if !UNSAFE
					return content;
				}
				catch (Exception ex)
				{
					_log.Fatal("LoadFile(" + aFile + ")", ex);
				}
#endif
			}
#if !UNSAFE
			return "";
#endif
		}

		public byte[] LoadFile(string aFile)
		{
			if (_dicByte.ContainsKey(aFile))
			{
				return _dicByte[aFile];
			}
			else
			{
#if !UNSAFE
				try
				{
#endif
#if DEBUG
					return File.ReadAllBytes("./Resources" + aFile);
#else
					Assembly assembly = Assembly.GetAssembly(typeof(FileLoader));
					string name = "XG." + assembly.GetName().Name + ".Resources" + aFile.Replace('/', '.');
					_dicByte.Add(aFile, new BinaryReader(assembly.GetManifestResourceStream(name)).ReadAllBytes());
					return _dicByte[aFile];
#endif
#if !UNSAFE
				}
				catch (Exception ex)
				{
					_log.Fatal("LoadFile(" + aFile + ")", ex);
				}
#endif
			}
#if !UNSAFE
			return new byte[0];
#endif
		}

		string PatchLanguage(string aContent, string[] aLanguages)
		{
			string lng = aLanguages[0].Substring(0, 2);
			return aContent.Replace("#LANGUAGE_SHORT#", lng);
		}

		public byte[] LoadImage(string aFile)
		{
			if (_dicByte.ContainsKey(aFile))
			{
				return _dicByte[aFile];
			}
			else
			{
#if !UNSAFE
				try
				{
#endif
					Assembly assembly = Assembly.GetAssembly(typeof(FileLoader));
					string name = "XG." + assembly.GetName().Name + ".Resources" + aFile.Replace('/', '.');
					Stream stream = assembly.GetManifestResourceStream(name);
					byte[] data = new byte[stream.Length];
					int offset = 0;
					int remaining = data.Length;
					while (remaining > 0)
					{
						int read = stream.Read(data, offset, remaining);
						if (read <= 0) { throw new EndOfStreamException(String.Format("End of stream reached with {0} bytes left to read", remaining)); }
						remaining -= read;
						offset += read;
					}
					_dicByte.Add(aFile, data);
					return _dicByte[aFile];
#if !UNSAFE
				}
				catch (Exception ex)
				{
					_log.Fatal("LoadImage(" + aFile + ")", ex);
				}
#endif
			}
#if !UNSAFE
			return null;
#endif
		}
	}

	public static class BinaryReaderExtension
	{
		public static byte[] ReadAllBytes(this BinaryReader reader)
		{
			const int bufferSize = 4096;
			using (var ms = new MemoryStream())
			{
				byte[] buffer = new byte[bufferSize];
				int count;
				while ((count = reader.Read(buffer, 0, buffer.Length)) != 0)
				{
					ms.Write(buffer, 0, count);
				}
				return ms.ToArray();
			}
			
		}
	}
}
