using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Paymentech.Core
{
    public enum ProfileAction
    {
        None=0,
        Create=1,
        Update=2,
        Suspend=3,
        Delete=4,
        Inactivate=5,
        Fetch=6
    }
    public class ProfileResponse:CustomerProfile
    {
        
        public bool Success { get { return String.IsNullOrEmpty(ErrorMessage); } }
        public string ErrorMessage { get; set; }
        
        public ProfileAction ProfileAction { get; set; }

        
    }
}
