using OpenerCreator.Helpers;
using OpenerCreator.Managers;

namespace OpenerCreatorTests
{
    public class ActionsMock : IActionManager
    {
        public ActionsMock() { }
        public string GetActionName(uint action) => "Ayylmao";

        public bool SameActionsByName(string action1, uint action2) => true;
    }
    public class OpenerManagerTests
    {
        [Fact]
        public void Compare_WhenOpenerExecutedPerfectly_ShouldAddSuccessMessage()
        {
            // Arrange
            var openerManager = OpenerManager.Instance(new ActionsMock());
            var used = new List<uint> { 3574, 152, 153, 3577, 7421, 3577, 357 };
            var feedback = new Feedback();
            openerManager.Loaded = used;

            // Act
            openerManager.Compare(used, (f) => { feedback = f; }, (_) => { });

            // Assert
            var successMessages = feedback.GetList().Where(m => m.Item1 == Feedback.MessageType.Success);
            Assert.Single(successMessages);
            Assert.Single(feedback.GetList());
        }
        /*
[Fact]
public void Compare_WhenOpenerHasDifference_ShouldAddErrorMessageAndInvokeWrongAction()
{
    // Arrange
    var openerManager = new OpenerManager(Actions.Instance);
    var used = new List<uint> { // data for a differing execution };
    var feedback = new Feedback();

    // Act
    openerManager.Compare(used, feedback.AddMessage, Assert.Fail);

    // Assert
    var successMessages = feedback.GetMessages();
    Assert.Contains((Feedback.MessageType.Error, // expected error message), successMessages);
}

[Fact]
public void Compare_WhenOpenerShifted_ShouldAddInfoMessage()
{
    // Arrange
    var openerManager = new OpenerManager(Actions.Instance);
    var used = new List<uint> { // data for a shifted execution};
    var feedback = new Feedback();

    // Act
    openerManager.Compare(used, feedback.AddMessage, Assert.Fail);

    // Assert
    var successMessages = feedback.GetMessages();
    Assert.Contains((Feedback.MessageType.Info, "You shifted your opener by 1 actions."), successMessages);
}
*/
    }
}
