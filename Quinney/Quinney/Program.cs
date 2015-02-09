using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace Quinney
{
	class Program
	{
		static void Main(string[] args)
		{
			Game.OnGameStart += Load;
		}

		private static void Load(EventArgs args)
		{
			
		}
	}
}
