using System;
using System.Collections.Generic;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace SolidworksAddinFramework
{
    public abstract class ReactiveObjectPropertyManagerPage<T> : PropertyManagerPageBase
        where T : ReactiveUI.ReactiveObject
    {
        protected T Data { get; }
        private T _Original;


        protected ReactiveObjectPropertyManagerPage
            (string name
                , IEnumerable<swPropertyManagerPageOptions_e> optionsE
                , ISldWorks swApp
                , IModelDoc2 modelDoc
                , T data) : base(name, optionsE, swApp, modelDoc)
        {
            Data = data;
        }

        public sealed override void Show()
        {
            _Original = Json.Clone(Data);
            base.Show();
        }

        protected override IDisposable PushSelections()
        {
            return ModelDoc.PushSelections(Data);
        }

        protected override void OnClose(swPropertyManagerPageCloseReasons_e reason)
        {
            switch (reason)
            {
                case swPropertyManagerPageCloseReasons_e.swPropertyManagerPageClose_Cancel:
                case swPropertyManagerPageCloseReasons_e.swPropertyManagerPageClose_UnknownReason:
                case swPropertyManagerPageCloseReasons_e.swPropertyManagerPageClose_UserEscape:
                //case swPropertyManagerPageCloseReasons_e.swPropertyManagerPageClose_Closed:
                //case swPropertyManagerPageCloseReasons_e.swPropertyManagerPageClose_ParentClosed:
                    using (Data.DelayChangeNotifications())
                    {
                        Json.Copy(_Original, Data);
                    }
                    break;
                case swPropertyManagerPageCloseReasons_e.swPropertyManagerPageClose_Okay:
                case swPropertyManagerPageCloseReasons_e.swPropertyManagerPageClose_Apply:
                default:
                    break;
            }
        }
    }
}