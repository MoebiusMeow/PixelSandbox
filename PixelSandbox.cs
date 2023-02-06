using PixelSandbox.Configs;
using System.Threading.Tasks;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PixelSandbox
{
	public class PixelSandbox : Mod
	{
		static public bool DebugMode => ModContent.GetInstance<SandboxConfig>().EnableDebug;
		static public PixelSandbox Instance {  get { return ModContent.GetInstance<PixelSandbox>(); } }

		static public string ModTranslate(string raw, string prefix = "") { return Language.GetTextValue("Mods." + Instance.Name + "." + prefix + raw); }
		static public LocalizedText ModTranslateL(string raw, string prefix = "") { return Language.GetText("Mods." + Instance.Name + "." + prefix + raw); }
	}
}