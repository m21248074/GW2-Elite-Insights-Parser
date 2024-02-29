using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GW2EIParserCommons;
using static GW2EIParser.MainForm;

namespace GW2EIParser
{
    internal enum OperationState
    {
        Ready = 0,
        Parsing = 1,
        Cancelling = 2,
        Complete = 3,
        Pending = 4,
        ClearOnCancel = 5,
        Queued = 6,
        UnComplete = 7,
    }
    internal class FormOperationController : OperationController
    {

        private CancellationTokenSource _cancelTokenSource;

        private Task _task;

        private readonly DataGridView _dgv;
        private readonly BindingSource _bdSrc;
        /// <summary>
        /// State of the button
        /// </summary>
        public string ButtonText { get; protected set; }
        /// <summary>
        /// State of the reparse button
        /// </summary>
        public string ReParseText { get; protected set; }
        /// <summary>
        /// Operation state
        /// </summary>
        public OperationState State { get; protected set; }

        public FormOperationController(string location, string status, DataGridView dgv, BindingSource bindingSource) : base(location, status)
        {
            ButtonText = "解析";
            State = OperationState.Ready;
            _dgv = dgv;
            bindingSource.Add(this);
            _bdSrc = bindingSource;
            SetReparseButtonState(false);
        }

        public void SetContext(CancellationTokenSource cancelTokenSource, Task task)
        {
            _cancelTokenSource = cancelTokenSource;
            _task = task;
        }

        public bool IsBusy()
        {
            if (_task != null)
            {
                return !_task.IsCompleted;
            }
            return false;
        }

        protected override void ThrowIfCanceled()
        {
            if (_task != null && _cancelTokenSource.IsCancellationRequested)
            {
                _cancelTokenSource.Token.ThrowIfCancellationRequested();
            }
        }

        private void InvalidateDataView()
        {
            if (_dgv.InvokeRequired)
            {
                _dgv.Invoke(new Action(() => _dgv.Invalidate()));
            }
            else
            {
                _dgv.Invalidate();
            }
        }

        private void SetReparseButtonState(bool onOff)
        {
            int rowIndex = _bdSrc.IndexOf(this);
            ReParseText = onOff ? "重新解析" : "不適用";
            if (rowIndex >= 0)
            {
                var reparseButton = (DataGridViewDisableButtonCell)_dgv.Rows[rowIndex].Cells["ReParseButtonState"];
                reparseButton.Enabled = onOff;
            }
        }

        public void ToRunState()
        {
            ButtonText = "取消";
            SetReparseButtonState(false);
            State = OperationState.Parsing;
            Status = "解析中";
            InvalidateDataView();
        }

        public void ToCancelState()
        {
            if (_task == null)
            {
                return;
            }
            State = OperationState.Cancelling;
            ButtonText = "取消中";
            SetReparseButtonState(false);
            _cancelTokenSource.Cancel();
            InvalidateDataView();
        }
        public void ToRemovalFromQueueState()
        {
            ToCancelState();
            Status = "Awaiting Removal from Queue";
            InvalidateDataView();
        }
        public void ToCancelAndClearState()
        {
            ToCancelState();
            State = OperationState.ClearOnCancel;
        }
        public void ToReadyState()
        {
            State = OperationState.Ready;
            ButtonText = "解析";
            SetReparseButtonState(false);
            Status = "Ready To Parse";
            InvalidateDataView();
        }

        public void ToCompleteState()
        {
            State = OperationState.Complete;
            ButtonText = "打開";
            SetReparseButtonState(true);
            FinalizeStatus("Parsing Successful - ");
            InvalidateDataView();
        }

        public void ToUnCompleteState()
        {
            State = OperationState.UnComplete;
            ButtonText = "解析";
            SetReparseButtonState(false);
            FinalizeStatus("Parsing Failure - ");
            InvalidateDataView();
        }

        public void ToPendingState()
        {
            State = OperationState.Pending;
            ButtonText = "取消";
            SetReparseButtonState(false);
            Status = "Pending";
            InvalidateDataView();
        }

        public void ToQueuedState()
        {
            State = OperationState.Queued;
            ButtonText = "取消";
            SetReparseButtonState(false);
            Status = "Queued";
            InvalidateDataView();
        }
    }
}
