using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using IServiceProvider = System.IServiceProvider;

namespace MattManela.TextFormatPicker
{
    public class TextInformationManager
    {
        public SelectedTextSymbol SelectedSymbol { get; set; }
        public EventHandler RefreshSelection;
        public const string TextEditorFontCategoryGuidString = "A27B4E24-A735-4D1D-B8E7-9716E1E3D8E0";
        Guid TextEditorFontCategoryGuid = new Guid(TextEditorFontCategoryGuidString);

        private readonly IVsFontAndColorStorage fontAndColorStorage;
        private readonly IVsFontAndColorUtilities fontAndColorUtilities;
        private readonly IVsFontAndColorCacheManager fontAndColorCache;
        private readonly IServiceProvider serviceProvider;
        private readonly IComponentModel componentModel;
        private readonly IVsEditorAdaptersFactoryService editorAdaptersFactoryService;
        private readonly ITextStructureNavigatorSelectorService textStructureNavigatorSelectorService;
        private readonly IClassificationFormatMapService classicationFormatMapService;
        private readonly IClassifierAggregatorService classificationAggregatorService;
        private readonly IVsTextManager textManager;
        private readonly Dictionary<string, ViewContext> DocumentMap = new Dictionary<string, ViewContext>();
        private string CurrentItemId { get; set; }
        private ViewContext CurrentViewContext { get; set; }

        public TextInformationManager(IServiceProvider serviceProvider, IComponentModel componentModel)
        {
            this.serviceProvider = serviceProvider;
            this.componentModel = componentModel;
            fontAndColorStorage = (IVsFontAndColorStorage) serviceProvider.GetService(typeof (SVsFontAndColorStorage));
            fontAndColorUtilities = fontAndColorStorage as IVsFontAndColorUtilities;
            fontAndColorCache = (IVsFontAndColorCacheManager)serviceProvider.GetService(typeof(SVsFontAndColorCacheManager));
            textManager = (IVsTextManager) serviceProvider.GetService(typeof (SVsTextManager));
            editorAdaptersFactoryService = componentModel.GetService<IVsEditorAdaptersFactoryService>();
            textStructureNavigatorSelectorService = componentModel.GetService<ITextStructureNavigatorSelectorService>();
            classicationFormatMapService = componentModel.GetService<IClassificationFormatMapService>();
            classificationAggregatorService = componentModel.GetService<IClassifierAggregatorService>();
        }

        public void SetupCommandFilter(IVsTextView view, Action action)
        {
            CommandFilter filter = new CommandFilter(action);
            IOleCommandTarget originalFilter;
            ErrorHandler.ThrowOnFailure(view.AddCommandFilter(filter, out originalFilter));
            filter.Init(originalFilter);
        }

        public void ManageActiveView(string itemId, IVsTextView vsTextView)
        {
            CurrentItemId = itemId;
            if (!DocumentMap.ContainsKey(itemId))
            {
                var wpfTextView = editorAdaptersFactoryService.GetWpfTextView(vsTextView);
                DocumentMap[itemId] = new ViewContext {VsTextView = vsTextView, WpfTextView = wpfTextView};
                SetupCommandFilter(vsTextView, () => DisplaySelectedTextProperties(itemId));
            }

            CurrentViewContext = DocumentMap[itemId];
        }

        private void OnRefreshSelection()
        {
            if (RefreshSelection != null)
                RefreshSelection(null, null);
        }

        public void DisplaySelectedTextProperties(string itemId)
        {
            if (CurrentItemId != itemId) return;

            var wpfTextView = CurrentViewContext.WpfTextView;
            var navigator = textStructureNavigatorSelectorService.GetTextStructureNavigator(wpfTextView.TextBuffer);

            SnapshotPoint activePoint;
            if (!wpfTextView.Selection.IsEmpty)
                activePoint = wpfTextView.Selection.Start.Position;
            else
                activePoint = wpfTextView.Caret.Position.BufferPosition;

            var currentWord = navigator.GetExtentOfWord(activePoint);
            if (!currentWord.Span.IsEmpty)
            {
                var classificationMap = classicationFormatMapService.GetClassificationFormatMap(wpfTextView);
                var classifier = classificationAggregatorService.GetClassifier(wpfTextView.TextBuffer);
                var classifications = classifier.GetClassificationSpans(currentWord.Span);
                var classification = classifications.FirstOrDefault();
                if (classification != null)
                {
                    var textProperties = classificationMap.GetTextProperties(classification.ClassificationType);

                    if (SelectedSymbol != null)
                        SelectedSymbol.PropertyChanged -= SelectedSymbol_PropertyChanged;


                    SelectedSymbol = new SelectedTextSymbol
                                         {
                                             ClassificationType = classification.ClassificationType,
                                             ClassificationMap = classificationMap,
                                             Symbol = currentWord.Span.GetText(),
                                             ClassificationName = classification.ClassificationType.Classification,
                                             BackgroundColor = (textProperties.BackgroundBrush as SolidColorBrush).Color,
                                             ForegroundColor = (textProperties.ForegroundBrush as SolidColorBrush).Color,
                                             Bold = textProperties.Bold,
                                             Italic = textProperties.Italic
                                         };
                    SelectedSymbol.PropertyChanged += SelectedSymbol_PropertyChanged;

                    OnRefreshSelection();
                }
            }
        }

        private void SelectedSymbol_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            fontAndColorStorage.OpenCategory(ref TextEditorFontCategoryGuid, (int) __FCSTORAGEFLAGS.FCSF_PROPAGATECHANGES |(int)__FCSTORAGEFLAGS.FCSF_LOADDEFAULTS);



            var symbol = sender as SelectedTextSymbol;
            symbol.ClassificationMap.BeginBatchUpdate();
            var props = symbol.ClassificationMap.GetTextProperties(symbol.ClassificationType);
            switch(e.PropertyName)
            {
                case "Bold":   
                    props = props.SetBold(symbol.Bold);
                    symbol.ClassificationMap.SetTextProperties(symbol.ClassificationType,props);

                    var colorInfo = new ColorableItemInfo[1];
                    fontAndColorStorage.GetItem(symbol.ClassificationName, colorInfo);
                    colorInfo[0].dwFontFlags |= (uint) FONTFLAGS.FF_BOLD;
                    fontAndColorStorage.SetItem(symbol.ClassificationName, colorInfo);
                    break;
                
                case "Italic":
                    props = props.SetItalic(symbol.Italic);
                    symbol.ClassificationMap.SetTextProperties(symbol.ClassificationType, props);
                    break;

                case "ForegroundColor":
                    props = props.SetForeground(symbol.ForegroundColor);
                    symbol.ClassificationMap.SetTextProperties(symbol.ClassificationType, props);
                    break;

                case "BackgroundColor":
                    props = props.SetBackground(symbol.BackgroundColor);
                    symbol.ClassificationMap.SetTextProperties(symbol.ClassificationType, props);
                    break;

                default:
                    break;
            }
            symbol.ClassificationMap.EndBatchUpdate();
            fontAndColorStorage.CloseCategory();
        }

        private class ViewContext
        {
            public IVsTextView VsTextView { get; set; }
            public IWpfTextView WpfTextView { get; set; }
        }
    }
}