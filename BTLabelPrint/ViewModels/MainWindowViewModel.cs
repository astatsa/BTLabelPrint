using Prism.Commands;
using Prism.Mvvm;
using Seagull.BarTender.Print;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace BTLabelPrint.ViewModels
{
    class MainWindowViewModel : BindableBase
    {
        #region Commands
        public ICommand PrintCommand => new DelegateCommand(
            () =>
            {
                using (Engine engine = new Engine())
                {
                    engine.Start();
                    var doc = engine.Documents.Open(@"C:\Users\Igor\Desktop\Документ1.btw");
                    doc.Close(SaveOptions.DoNotSaveChanges);
                }
            });
        #endregion
    }
}
