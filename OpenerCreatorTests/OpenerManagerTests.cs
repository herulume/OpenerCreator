using OpenerCreator.Helpers;
using OpenerCreator.Managers;

namespace OpenerCreatorTests
{
    public class ActionsMock : IActionManager
    {
        public ActionsMock() { }
        public string GetActionName(uint action) => action.ToString();

        public bool SameActionsByName(string action1, uint action2) => action1 == action2.ToString();
    }

    public class OpenerManagerTests
    {
        [Fact]
        public void Compare_WhenOpenerExecutedPerfectly_ShouldAddSuccessMessage()
        {
            // Arrange
            var openerManager = new OpenerManager(new ActionsMock());
            openerManager.Loaded = new List<uint> { 1, 2, 3, 1, 2 };
            var used = new List<uint> { 1, 2, 3, 1, 2 };
            var feedback = new Feedback();

            // Act
            openerManager.Compare(used, (f) => { feedback = f; }, (_) => { });

            // Assert
            var successMessages = feedback.GetList().Where(m => m.Item1 == Feedback.MessageType.Success);
            Assert.Single(successMessages);
            Assert.Single(feedback.GetList());
        }

        [Fact]
        public void Compare_WhenOpenerExecutedPerfectlyWithCatchAll_ShouldAddSuccessMessage()
        {
            // Arrange
            var openerManager = new OpenerManager(new ActionsMock());
            openerManager.Loaded = new List<uint> { 1, 2, 0, 1, 2 };
            var used = new List<uint> { 1, 2, 3, 1, 2 };
            var feedback = new Feedback();

            // Act
            openerManager.Compare(used, (f) => { feedback = f; }, (_) => { });

            // Assert
            var successMessages = feedback.GetList().Where(m => m.Item1 == Feedback.MessageType.Success);
            Assert.Single(successMessages);
            Assert.Single(feedback.GetList());
        }

        [Fact]
        public void Compare_WhenOpenerHasDifference_ShouldAddErrorMessageAndInvokeWrongAction()
        {
            // Arrange
            var openerManager = new OpenerManager(new ActionsMock());
            openerManager.Loaded = new List<uint> { 1, 2, 3, 0, 2 };
            var used = new List<uint> { 1, 5, 3, 1, 1 };
            var feedback = new Feedback();

            // Act
            openerManager.Compare(used, (f) => { feedback = f; }, (_) => { });

            // Assert
            var errorMessages = feedback.GetList().Where(m => m.Item1 == Feedback.MessageType.Error);
            Assert.Equal(2, errorMessages.Count());
            Assert.Equal(2, feedback.GetList().Count);
        }

        [Fact]
        public void Compare_WhenOpenerShifted_ShouldAddInfoMessage()
        {
            // Arrange
            var openerManager = new OpenerManager(new ActionsMock());
            openerManager.Loaded = new List<uint> { 1, 2, 3, 0, 5, 6 };
            var used = new List<uint> { 1, 3, 4, 5, 6, 99 };
            var feedback = new Feedback();

            // Act
            openerManager.Compare(used, (f) => { feedback = f; }, (_) => { });

            // Assert
            var shiftMessages = feedback.GetList().Where(m => m.Item1 == Feedback.MessageType.Info);
            var errorMessages = feedback.GetList().Where(m => m.Item1 == Feedback.MessageType.Error);
            Assert.Single(errorMessages);
            Assert.Single(shiftMessages);
            Assert.Equal(2, feedback.GetList().Count);
            Assert.Contains("by 1 action", string.Join("\n", feedback.GetMessages()));
        }
    }
}
