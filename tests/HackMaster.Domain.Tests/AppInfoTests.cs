namespace HackMaster.Domain.Tests;

public class AppInfoTests
{
    [Fact]
    public void AppInfo_Name_Is_HackMaster_Companion__PRODUCTO()
    {
        Assert.Equal("HackMaster Companion", AppInfo.Name);
    }
}
