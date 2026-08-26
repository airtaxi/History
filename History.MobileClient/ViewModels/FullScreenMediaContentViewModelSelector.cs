namespace History.MobileClient.ViewModels
{
    public class FullScreenMediaContentViewModelSelector : DataTemplateSelector
    {
        public DataTemplate FullScreenVideoTemplate { get; set; }
        public DataTemplate FullScreenMediaContentTemplate { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            if (item is FullScreenMediaContentViewModel viewModel)
            {
#if IOS
                if (viewModel.CurrentMedia is VideoViewModel) return FullScreenVideoTemplate;
                else return FullScreenMediaContentTemplate;
#else
                return FullScreenMediaContentTemplate;
#endif
            }
            else throw new ArgumentException("Unknown item type", nameof(item));
        }
    }
}
