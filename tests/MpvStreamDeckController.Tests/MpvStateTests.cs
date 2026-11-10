using Shouldly;

namespace MpvStreamDeckController.Tests;

public class MpvStateTests
{
    [Fact]
    public void MpvState_DefaultValueForUnregisteredProperty_ShouldBeNull()
    {
        var state = new MpvState(1, false, new Dictionary<long, object>());

        state.Number(MpvProperties.TimePos).ShouldBeNull();
        state.Text(MpvProperties.TimePos).ShouldBeNull();
        state.Flag(MpvProperties.TimePos).ShouldBeNull();
    }

    [Fact]
    public void MpvState_Number_ShouldBeNull()
    {
        var propertyValue = 300d;

        var values = new Dictionary<long, object>
        {
            { MpvProperties.TimePos.Id, propertyValue  }
        };

        var state = new MpvState(1, false, values);

        state.Number(MpvProperties.TimePos).ShouldBe(propertyValue);
        state.Text(MpvProperties.TimePos).ShouldBeNull();
        state.Flag(MpvProperties.TimePos).ShouldBeNull();
    }

    [Fact]
    public void MpvState_Text_ShouldBeNull()
    {
        var propertyValue = "lipsum";

        var values = new Dictionary<long, object>
        {
            { MpvProperties.ACompressor.Id, propertyValue  }
        };

        var state = new MpvState(1, false, values);

        state.Number(MpvProperties.ACompressor).ShouldBeNull();
        state.Text(MpvProperties.ACompressor).ShouldBe(propertyValue);
        state.Flag(MpvProperties.ACompressor).ShouldBeNull();
    }

    [Fact]
    public void MpvState_Flag_ShouldBeNull()
    {
        var propertyValue = true;

        var values = new Dictionary<long, object>
        {
            { MpvProperties.Mute.Id, propertyValue  }
        };

        var state = new MpvState(1, false, values);

        state.Number(MpvProperties.Mute).ShouldBeNull();
        state.Text(MpvProperties.Mute).ShouldBeNull();
        state.Flag(MpvProperties.Mute).ShouldBe(true);
    }
}
