using TeleBotService.Config;

namespace TeleBotService.Tests;

public class ScheduleConfigTests
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

        Assert.Equal(EventTriggerData.Empty, config.EventTriggerInfo);
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

    [Theory]
    [InlineData("TestEventName", null)]
    [InlineData("TestEventName:MeetCount=3", 3)]
    [InlineData("TestEventName:Param1=Val1;Param2=Val2", null)]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22", null)]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22;MeetCount=777", 777)]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22;MeetCount=caca", null)]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22;ValidTimeRange=777", null)]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22;ValidTimeRange=00:33-13:44", null)]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22;ValidTimeRange=13:44-09:66", null)]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;ExceptParam2=ValNot1|ValNot2|ValNot3;Param3=00:22;ValidTimeRange=13:44-09:66", null)]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22;ValidTimeRange=13:44-_", null)]
    [InlineData("JacunaMatata:Param1=11:11;Param2=Val2;Param3=00:22;ValidTimeRange=_-13:44", null)]
    [InlineData(":Param1=11:11;Param2=Val2;Param3=00:22;MeetCount=0", 0)]
    [InlineData(":CACA;Param1=11:11;Param2=Val2;Param3=00:22", null)]
    [InlineData(null, null)]
    [InlineData(":", null)]
    [InlineData(":MeetCount=333", 333)]
    [InlineData(":CACA", null)]
    [InlineData("", null)]
    public void ScheduleConfigTest_EventTriggerInfo_HasCorrectMeetCount(string? eventTriggerString, int? expectedValue)
    {
        var config = new ScheduleConfig
        {
            CommandText = string.Empty,
            User = string.Empty,
            EventTrigger = eventTriggerString,
        };

        Assert.Equal(expectedValue, config.EventTriggerInfo.MeetCount);
    }

    [Theory]
    [InlineData("TestEventName", null)]
    [InlineData("TestEventName:MeetCount=3;PrevMeetCount=6", 6)]
    [InlineData("TestEventName:Param1=Val1;Param2=Val2", null)]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22", null)]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22;PrevMeetCount=777", 777)]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22;PrevMeetCount=caca", null)]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22;ValidTimeRange=777", null)]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22;ValidTimeRange=00:33-13:44", null)]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22;ValidTimeRange=13:44-09:66", null)]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;ExceptParam2=ValNot1|ValNot2|ValNot3;Param3=00:22;ValidTimeRange=13:44-09:66", null)]
    [InlineData("TestEventName:Param1=11:11;Param2=Val2;Param3=00:22;ValidTimeRange=13:44-_", null)]
    [InlineData("JacunaMatata:Param1=11:11;Param2=Val2;Param3=00:22;ValidTimeRange=_-13:44", null)]
    [InlineData(":Param1=11:11;Param2=Val2;Param3=00:22;MeetCount=0;PrevMeetCount=3", 3)]
    [InlineData(":CACA;Param1=11:11;Param2=Val2;Param3=00:22", null)]
    [InlineData(null, null)]
    [InlineData(":", null)]
    [InlineData(":PrevMeetCount=333", 333)]
    [InlineData(":CACA", null)]
    [InlineData("", null)]
    public void ScheduleConfigTest_EventTriggerInfo_HasCorrectPrevMeetCount(string? eventTriggerString, int? expectedValue)
    {
        var config = new ScheduleConfig
        {
            CommandText = string.Empty,
            User = string.Empty,
            EventTrigger = eventTriggerString,
        };

        Assert.Equal(expectedValue, config.EventTriggerInfo.PrevMeetCount);
    }

    [Theory]
    [InlineData("TestEventName", "ExceptParam1", "Excluded", false)]
    [InlineData("TestEventName:MeetCount=3;PrevMeetCount=6", "Param1", "Excluded", false)]
    [InlineData("TestEventName:Param1=Val1;Param2=Val2;ExceptParam2=Val1|Val2|Val3", "Param2", "Val0", false)]
    [InlineData("TestEventName:Param1=Val1;Param2=Val2;ExceptParam2=Val1|Val2|Val3", "Param2", "Val1", true)]
    [InlineData("TestEventName:Param1=Val1;Param2=Val2;ExceptParam2=Val1|Val2|Val3", "Param2", "Val2", true)]
    [InlineData("TestEventName:Param1=Val1;Param2=Val2;ExceptParam2=Val1|Val2|Val3", "Param2", "Val3", true)]
    [InlineData("TestEventName:Param1=Val1;Param2=Val2;ExceptParam2=Val1|Val2|Val3", "Param2", "Val4", false)]
    [InlineData("TestEventName:Param1=Val1;Param2=Val2;ExceptParam1=ValP10|ValP12|ValP14", "Param1", "ValP10", true)]
    [InlineData("TestEventName:Param1=Val1;Param2=Val2;ExceptParam1=ValP10|ValP12|ValP14", "Param1", "ValP11", false)]
    [InlineData("TestEventName:Param1=Val1;Param2=Val2;ExceptParam1=ValP10|ValP12|ValP14", "Param1", "ValP12", true)]
    [InlineData("TestEventName:Param1=Val1;Param2=Val2;ExceptParam1=ValP10|ValP12|ValP14", "Param1", "ValP13", false)]
    [InlineData("TestEventName:Param1=Val1;Param2=Val2;ExceptParam1=ValP10|ValP12|ValP14", "Param1", "ValP14", true)]
    public void ScheduleConfigTest_EventTriggerInfo_IsExcluded_ReturnsCorrectValue(string? eventTriggerString, string paramName, string? paramValue, bool expectedResult)
    {
        var config = new ScheduleConfig
        {
            CommandText = string.Empty,
            User = string.Empty,
            EventTrigger = eventTriggerString,
        };

        Assert.Equal(expectedResult, config.EventTriggerInfo.IsExcluded(paramName, paramValue));
    }

    [Theory]
    [InlineData("TestEventName", "ExceptParam1", "Excluded", true)]
    [InlineData("TestEventName:MeetCount=3;PrevMeetCount=6", "Param1", "Excluded", true)]
    [InlineData("TestEventName:Param1=Val1;Param2=Val2;ExceptParam2=Val1|Val2|Val3", "Param2", "Val0", false)]
    [InlineData("TestEventName:Param1=Val1;Param2=Val2;ExceptParam2=Val1|Val2|Val3", "Param2", "Val1", false)]
    [InlineData("TestEventName:Param1=Val1;Param2=Val2;ExceptParam2=Val1|Val2|Val3", "Param2", "Val2", false)]
    [InlineData("TestEventName:Param1=Val1;Param2=Val2;ExceptParam2=Val1|Val2|Val3", "Param2", "Val3", false)]
    [InlineData("TestEventName:Param1=Val1;Param2=Val2;ExceptParam2=Val1|Val2|Val3", "Param2", "Val4", false)]
    [InlineData("TestEventName:Param1=Val1;Param2=Val2;ExceptParam1=ValP10|ValP12|ValP14", "Param1", "ValP10", false)]
    [InlineData("TestEventName:Param1=Val1;Param2=Val2;ExceptParam1=ValP10|ValP12|ValP14", "Param1", "ValP11", false)]
    [InlineData("TestEventName:Param1=ValP12;Param2=Val2;ExceptParam1=ValP10|ValP122|ValP14", "Param1", "ValP12", true)]
    [InlineData("TestEventName:Param1=Val1;Param2=Val2;ExceptParam1=ValP10|ValP12|ValP14", "Param1", "ValP13", false)]
    [InlineData("TestEventName:Param1=Val1;Param2=Val2;ExceptParam1=ValP10|ValP12|ValP14", "Param1", "ValP14", false)]
    [InlineData("TestEventName:Param1=Val1;Param2=Val2;ExceptParam2=Val1|Val2|Val3", "Param1", "Val1", true)]
    [InlineData("TestEventName:Param1=Val1;Param2=Val2;ExceptParam2=Val1|Val3", "Param2", "Val2", true)]
    [InlineData("TestEventName:Param1=Val1;ExceptParam1=Val1", "Param1", "Val1", false)]
    [InlineData(null, "Param2", "Val2", true)]
    [InlineData("", "Param2", "Val2", true)]
    public void ScheduleConfigTest_EventTriggerInfo_HasParamValueOrNotSet_ReturnsCorrectValue(string? eventTriggerString, string paramName, string? paramValue, bool expectedResult)
    {
        var config = new ScheduleConfig
        {
            CommandText = string.Empty,
            User = string.Empty,
            EventTrigger = eventTriggerString,
        };

        Assert.Equal(expectedResult, config.EventTriggerInfo.HasParamValueOrNotSet(paramName, paramValue));
    }
}