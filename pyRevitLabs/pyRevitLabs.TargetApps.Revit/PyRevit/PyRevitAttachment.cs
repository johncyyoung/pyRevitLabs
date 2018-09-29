using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pyRevitLabs.TargetApps.Revit {
    public class PyRevitAttachment {
        public PyRevitAttachment(RevitProduct product, PyRevitClone clone, PyRevitAttachmentType attachmentType) {
            Product = product;
            Clone = clone;
            AttachmentType = attachmentType;
        }

        public RevitProduct Product { get; private set; }
        public PyRevitClone Clone { get; private set; }
        public PyRevitAttachmentType AttachmentType { get; private set; }
        public bool AllUsers {
            get {
                return AttachmentType == PyRevitAttachmentType.AllUsers;
            }
        }

    }
}
