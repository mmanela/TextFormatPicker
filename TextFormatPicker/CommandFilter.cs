using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;

namespace MattManela.TextFormatPicker
{
    [ComVisible(true)]
    public class CommandFilter : IOleCommandTarget
    {
        private readonly Action actionInView;
        private readonly TextInformationManager manager;
        private IOleCommandTarget oldFilter;

        public CommandFilter(Action actionInView)
        {
            this.actionInView = actionInView;
            this.manager = manager;
        }

        public void Init(IOleCommandTarget currentFilter)
        {
            oldFilter = currentFilter;
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            int hr = (int)Constants.OLECMDERR_E_NOTSUPPORTED;
            if (pguidCmdGroup == VSConstants.VSStd2K)
            {
                switch (nCmdID)
                {
                    case (uint)VSConstants.VSStd2KCmdID.SHOWCONTEXTMENU:
                        {
                            actionInView();
                            hr = VSConstants.S_OK;
                            break;
                        }
                    case (uint)VSConstants.VSStd2KCmdID.ECMD_LEFTCLICK:
                        {
                            actionInView();
                            break;
                        }
                    default:
                        {
                            //this is a command we arent intercepting so forward it
                            hr = oldFilter.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                            break;
                        }
                }
            }
            else
            {
                hr = oldFilter.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            }

            return hr;
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (pguidCmdGroup == VSConstants.VSStd2K && prgCmds != null)
            {
                switch (prgCmds[0].cmdID)
                {
                    case (uint)VSConstants.VSStd2KCmdID.SHOWCONTEXTMENU:
                        {
                            prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_SUPPORTED | (uint)OLECMDF.OLECMDF_ENABLED;
                            return VSConstants.S_OK;
                        }
                    case (uint)VSConstants.VSStd2KCmdID.ECMD_LEFTCLICK:
                        {
                            prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_SUPPORTED | (uint)OLECMDF.OLECMDF_ENABLED;
                            return VSConstants.S_OK;
                        }
                    default:
                        {
                            return oldFilter.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
                        }
                }
            }

            else
            {
                return oldFilter.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
            }
        }
    }
}