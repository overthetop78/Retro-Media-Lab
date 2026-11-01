using RetroMediaLab.App.Dictionaries.Localization;
using RetroMediaLab.Tests.Application.TestInfrastructure;
namespace RetroMediaLab.Tests.Interface.Localization;
[Collection("WPF")]
public class LocalizationTests(StaExecutionScenarios sta)
{
    public static IEnumerable<object[]> Languages()=>UiLanguageCatalog.Available.Select(x=>new object[]{x.Code});
    [Theory][MemberData(nameof(Languages))]
    public Task ResourceKeysResolveAfterLanguageChange(string language)=>sta.Run(()=>LocalizedViewsScenarios.Change(language));
    [Theory] [InlineData("fr-FR")] [InlineData("de-DE")] [InlineData("ar-SA")] [InlineData("he-IL")]
    public Task LongTextAndTextDirectionPreserveCommands(string language) => sta.Run(() => LocalizedViewsScenarios.Layout(language));
}
