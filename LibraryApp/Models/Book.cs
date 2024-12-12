using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LibraryApp.Models
{
    public class Book(string name, string autor, int year, string publisher)
    {
        public Guid id {  get; set; }
        public string name { get; set; }
        public string autor { get; set; }
        public int year { get; set; }
        public string publisher { get; set; }


        public override string ToString() =>
          $@"Название: {name}
Автор: {autor}
Год: {year}\n"; //Копий на складе: {bookCount}";
    
    
    }
}
