using TeleBotService.Config;

namespace TeleBotService.Tests;

public class ScheduleConfigTest
{
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ScheduleConfigTest_EventTriggerInfo_IsEmpty(string? eventTriggerString)
    {
        var config = new ScheduleConfig
        {
            CommandText = string.Empty,
            User = string.Empty,
            EventTrigger = eventTriggerString,
        };

        Assert.Equal(ScheduleConfig.EventTriggerData.Empty, config.EventTriggerInfo);
    }

    [Theory]
    [InlineData("TestEventName", "TestEventName")]
    [InlineData("TestEventName:", "TestEventName")]
    [InlineData("TestEventName:Param1=Val1;Param2=Val2", "TestEventName")]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22", "TestEventName")]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22;Delay=777", "TestEventName")]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22;Delay=caca", "TestEventName")]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22;ValidTimeRange=777", "TestEventName")]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22;ValidTimeRange=00:33-13:44", "TestEventName")]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22;ValidTimeRange=13:44-09:66", "TestEventName")]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;ExceptParam2=ValNot1|ValNot2|ValNot3;Param3=00:22;ValidTimeRange=13:44-09:66", "TestEventName")]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22;ValidTimeRange=13:44-_", "TestEventName")]
    [InlineData("JacunaMatata:Param1=11:11;Param2=Val2;Param3=00:22;ValidTimeRange=_-13:44", "JacunaMatata")]
    [InlineData(":Param1=11:11;Param2=Val2;Param3=00:22", "")]
    [InlineData(":CACA;Param1=11:11;Param2=Val2;Param3=00:22", "")]
    [InlineData(null, "")]
    [InlineData(":", "")]
    [InlineData(":CACA", "")]
    [InlineData("", "")]
    public void ScheduleConfigTest_EventTriggerInfo_HasCorrectEventName(string? eventTriggerString, string expectedEventName)
    {
        var config = new ScheduleConfig
        {
            CommandText = string.Empty,
            User = string.Empty,
            EventTrigger = eventTriggerString,
        };

        Assert.Equal(expectedEventName, config.EventTriggerInfo.EventName);
    }

    [Theory]
    [InlineData("TestEventName")]
    [InlineData("TestEventName:")]
    [InlineData("TestEventName:Param1=Val1;Param2=Val2")]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22")]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22;Delay=777")]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22;Delay=caca")]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22;ValidTimeRange=777")]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22;ValidTimeRange=00:33-13:44")]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22;ValidTimeRange=13:44-09:66")]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;ExceptParam2=ValNot1|ValNot2|ValNot3;Param3=00:22;ValidTimeRange=13:44-09:66")]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22;ValidTimeRange=13:44-_")]
    [InlineData("JacunaMatata:Param1=11:11;Param2=Val2;Param3=00:22;ValidTimeRange=_-13:44")]
    [InlineData(":Param1=11:11;Param2=Val2;Param3=00:22")]
    [InlineData(":CACA;Param1=11:11;Param2=Val2;Param3=00:22")]
    [InlineData(null)]
    [InlineData(":")]
    [InlineData(":CACA")]
    [InlineData("")]
    public void ScheduleConfigTest_EventTriggerInfo_OnlyOneInstance(string? eventTriggerString)
    {
        var config = new ScheduleConfig
        {
            CommandText = string.Empty,
            User = string.Empty,
            EventTrigger = eventTriggerString,
        };


        var ti1 = config.EventTriggerInfo;
        var ti2 = config.EventTriggerInfo;
        var ti3 = config.EventTriggerInfo;
        var ti4 = config.EventTriggerInfo;
        var ti5 = config.EventTriggerInfo;

        Assert.True(ReferenceEquals(ti1, ti2));
        Assert.True(ReferenceEquals(ti2, ti3));
        Assert.True(ReferenceEquals(ti3, ti4));
        Assert.True(ReferenceEquals(ti4, ti5));
    }

    [Theory]
    [InlineData("TestEventName", null)]
    [InlineData("TestEventName:", null)]
    [InlineData("TestEventName:Param1=Val1;Param2=Val2", null)]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22", null)]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22;Delay=777", 777)]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22;Delay=caca", null)]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22;ValidTimeRange=777", null)]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22;ValidTimeRange=00:33-13:44", null)]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22;ValidTimeRange=13:44-09:66", null)]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;ExceptParam2=ValNot1|ValNot2|ValNot3;Param3=00:22;ValidTimeRange=13:44-09:66", null)]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22;ValidTimeRange=13:44-_", null)]
    [InlineData("JacunaMatata:Param1=11:11;Param2=Val2;Param3=00:22;ValidTimeRange=_-13:44", null)]
    [InlineData(":Param1=11:11;Param2=Val2;Param3=00:22", null)]
    [InlineData(":CACA;Param1=11:11;Param2=Val2;Param3=00:22", null)]
    [InlineData(null, null)]
    [InlineData(":", null)]
    [InlineData(":Delay=333", 333)]
    [InlineData(":CACA", null)]
    [InlineData("", null)]
    public void ScheduleConfigTest_EventTriggerInfo_HasCorrectDelay(string? eventTriggerString, int? expectedDelayInSeconds)
    {
        var config = new ScheduleConfig
        {
            CommandText = string.Empty,
            User = string.Empty,
            EventTrigger = eventTriggerString,
        };

        TimeSpan? expectedValuer = expectedDelayInSeconds.HasValue ? TimeSpan.FromSeconds(expectedDelayInSeconds.Value) : null;
        Assert.Equal(expectedValuer, config.EventTriggerInfo.Delay);
    }

    [Theory]
    [InlineData("TestEventName", null, null)]
    [InlineData("TestEventName:", null, null)]
    [InlineData("TestEventName:Param1=Val1;Param2=Val2", null, null)]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22", null, null)]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22;Delay=777", 777, null)]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22;Delay=caca", null, null)]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22;ValidTimeRange=777", null, null)]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22;ValidTimeRange=00:33-13:44", 0, 33)]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22;ValidTimeRange=13:44-09:55", 13, 44)]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;ExceptParam2=ValNot1|ValNot2|ValNot3;Param3=00:22;ValidTimeRange=13:28-09:66", 13, 28)]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22;ValidTimeRange=13:44-_", 13, 44)]
    [InlineData("JacunaMatata:Param1=11:11;Param2=Val2;Param3=00:22;ValidTimeRange=_-13:44", null, null)]
    [InlineData(":Param1=11:11;Param2=Val2;Param3=00:22", null, null)]
    [InlineData(":CACA;Param1=11:11;Param2=Val2;Param3=00:22", null, null)]
    [InlineData(null, null, null)]
    [InlineData(":", null, null)]
    [InlineData(":ValidTimeRange=07:55-09:35;Delay=333", 7, 55)]
    [InlineData(":CACA", null, null)]
    [InlineData("", null, null)]
    public void ScheduleConfigTest_EventTriggerInfo_HasCorrectStartValidTimeRange(string? eventTriggerString, int? expectedHours, int? expectedMinutes)
    {
        var config = new ScheduleConfig
        {
            CommandText = string.Empty,
            User = string.Empty,
            EventTrigger = eventTriggerString,
        };

        TimeOnly? expectdValue = expectedHours.HasValue && expectedMinutes.HasValue ? new TimeOnly(expectedHours.Value, expectedMinutes.Value) : null;
        Assert.Equal(expectdValue, config.EventTriggerInfo.StartValidTime);
    }

    [Theory]
    [InlineData("TestEventName", null, null)]
    [InlineData("TestEventName:", null, null)]
    [InlineData("TestEventName:Param1=Val1;Param2=Val2", null, null)]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22", null, null)]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22;Delay=777", 777, null)]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22;Delay=caca", null, null)]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22;ValidTimeRange=777", null, null)]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22;ValidTimeRange=00:33-13:44", 13, 44)]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22;ValidTimeRange=13:44-09:55", 9, 55)]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;ExceptParam2=ValNot1|ValNot2|ValNot3;Param3=00:22;ValidTimeRange=13:28-09:06", 9, 6)]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22;ValidTimeRange=13:44-_", null, null)]
    [InlineData("JacunaMatata:Param1=11:11;Param2=Val2;Param3=00:22;ValidTimeRange=_-13:44", 13, 44)]
    [InlineData(":Param1=11:11;Param2=Val2;Param3=00:22", null, null)]
    [InlineData(":CACA;Param1=11:11;Param2=Val2;Param3=00:22", null, null)]
    [InlineData(null, null, null)]
    [InlineData(":", null, null)]
    [InlineData(":ValidTimeRange=07:55-09:35;Delay=333", 9, 35)]
    [InlineData(":CACA", null, null)]
    [InlineData("", null, null)]
    public void ScheduleConfigTest_EventTriggerInfo_HasCorrectEndValidTimeRange(string? eventTriggerString, int? expectedHours, int? expectedMinutes)
    {
        var config = new ScheduleConfig
        {
            CommandText = string.Empty,
            User = string.Empty,
            EventTrigger = eventTriggerString,
        };

        TimeOnly? expectdValue = expectedHours.HasValue && expectedMinutes.HasValue ? new TimeOnly(expectedHours.Value, expectedMinutes.Value) : null;
        Assert.Equal(expectdValue, config.EventTriggerInfo.EndValidTime);
    }
}