using System;
using Inventor;

namespace BracketLibraryInventorPlugin.Interop
{
    /// <summary>
    /// Pick 1 ĐIỂM trên geometry (kèm entity dưới con trỏ) bằng <see cref="InteractionEvents"/>.
    /// <c>CommandManager.Pick</c> chỉ trả entity, không trả toạ độ điểm click — cần điểm để biết
    /// vị trí đặt bracket dọc theo cạnh Web∩TopPlate.
    ///
    /// Chạy ĐỒNG BỘ: <c>Start()</c> rồi bơm message (DoEvents) tới khi <c>OnSelect</c> hoặc
    /// <c>OnTerminate</c> (Esc / phải chuột ▸ Cancel). Trả null nếu user huỷ.
    /// </summary>
    internal sealed class PointOnEntityPicker
    {
        private readonly global::Inventor.Application _app;

        private bool _done;
        private Point _point;
        private object _entity;

        public PointOnEntityPicker(global::Inventor.Application app) => _app = app;

        public sealed class Result
        {
            public Point Point;
            public object Entity;
        }

        public Result Pick(string prompt, SelectionFilterEnum filter)
        {
            _done = false;
            _point = null;
            _entity = null;

            InteractionEvents ie = _app.CommandManager.CreateInteractionEvents();
            ie.StatusBarText = prompt;
            ie.OnTerminate += Ie_OnTerminate;

            SelectEvents se = ie.SelectEvents;
            se.AddSelectionFilter(filter);
            se.WindowSelectEnabled = false;
            se.SingleSelectEnabled = true;
            se.OnSelect += Se_OnSelect;

            ie.Start();
            try
            {
                int guard = 0;
                while (!_done && guard++ < 60000) // ~10 phút trần
                {
                    System.Windows.Forms.Application.DoEvents();
                    System.Threading.Thread.Sleep(10);
                }
            }
            finally
            {
                try { se.OnSelect -= Se_OnSelect; } catch { }
                try { ie.OnTerminate -= Ie_OnTerminate; } catch { }
                try { ie.Stop(); } catch { }
            }

            return _point == null ? null : new Result { Point = _point, Entity = _entity };
        }

        private void Se_OnSelect(ObjectsEnumerator JustSelectedEntities, SelectionDeviceEnum SelectionDevice,
                                 Point ModelPosition, Point2d ViewPosition, View View)
        {
            _point = ModelPosition;
            if (JustSelectedEntities != null && JustSelectedEntities.Count >= 1)
                _entity = JustSelectedEntities[1];
            _done = true;
        }

        private void Ie_OnTerminate() => _done = true;
    }
}
