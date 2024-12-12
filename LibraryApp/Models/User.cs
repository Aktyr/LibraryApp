using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryApp.Models
{
    class User(string lastName, string firstName, string middleName, string contactInfo)
    {
        public Guid id { get; set; }
        public string lastName { get; set; } //Фамилия
        public string firstName { get; set; } //Имя
        public string middleName { get; set; } //Отчетво
        public string contactInfo { get; set; }

        //public bool subscriptionStatus { get; set; }

    }
}
