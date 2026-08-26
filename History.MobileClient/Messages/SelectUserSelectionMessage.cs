using History.MobileClient.ViewModels;

namespace History.MobileClient.Messages;

public class SelectUserSelectionMessage(SelectUserViewModel user) : ValueDeletedMessage<SelectUserViewModel>(user);
