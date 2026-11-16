using Bunit;
using HackMaster.Web.Pages;

namespace HackMaster.Web.Tests;

public class HomePageTests : BunitContext
{
    [Fact]
    public void Home_Shows_App_Name__F000()
    {
        var cut = Render<Home>();

        Assert.Equal("HackMaster Companion", cut.Find("h1").TextContent.Trim());
    }
}
