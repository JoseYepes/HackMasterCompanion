using Bunit;
using HackMaster.Web.Pages;

namespace HackMaster.Web.Tests;

public class NotFoundPageTests : BunitContext
{
    [Fact]
    public void NotFound_Shows_Message_In_Spanish__F000()
    {
        var cut = Render<NotFound>();

        Assert.Equal("Página no encontrada", cut.Find("h1").TextContent.Trim());
    }
}
