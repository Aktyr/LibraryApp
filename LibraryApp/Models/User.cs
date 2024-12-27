namespace LibraryApp.Models
{
    class User(string lastName, string firstName, string middleName, string contactInfo)
    {
        public Guid id { get; set; } = Guid.NewGuid();
        public string lastName { get; set; } = lastName; //Фамилия
        public string firstName { get; set; } = firstName; //Имя
        public string middleName { get; set; } = middleName; //Отчетво
        public string contactInfo { get; set; } = contactInfo;

        //public bool subscriptionStatus { get; set; }

    }
}
