using SimpleNetAuth.Models.Config;
using SimpleNetAuth.Models.ViewModels;
using TypeGen.Core.SpecGeneration;

namespace SimpleNetAuth;

/// <summary>
/// This file exports C# classes into TypeScript classes of the Angular application.
/// Options are configured in the tgconfig.json file.
/// </summary>
public class TypeScriptExports : GenerationSpec
{
    public TypeScriptExports()
    {
        AddClass<AppConfig>();
        AddClass<LoginModel>();
        AddClass<RegisterModel>();
    }
}