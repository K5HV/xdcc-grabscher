// 
//  Helper.cs
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
using System.Text.RegularExpressions;

namespace XG.Server.Plugin.Core.Irc.Parser
{
	public static class Helper
	{
		public static string Magicstring = @"((\*|:){2,3}|->|<-|)";

		public static string RemoveSpecialIrcChars(string aData)
		{
			aData = Regex.Replace(aData, "(\u0002|\u0003)(\\d+(,\\d{1,2}|)|)", String.Empty);
			aData = aData.Replace("\u0001", String.Empty);
			aData = aData.Replace("\u000F", String.Empty);
			aData = aData.Replace("\uFFFD", String.Empty);
			aData = aData.Replace("\u0016", String.Empty);
			return aData.Trim();
		}

		public static bool Matches(string aMessage, string aRegex)
		{
			Match tMatch = Regex.Match(aMessage, aRegex, RegexOptions.IgnoreCase);
			return tMatch.Success;
		}
	}
}