namespace LibraryApp.Models;

public class User(string lastName, string firstName, string middleName, string contactInfo)
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string LastName { get; set; } = lastName; //Фамилия
    public string FirstName { get; set; } = firstName; //Имя
    public string MiddleName { get; set; } = middleName; //Отчетво
    public string ContactInfo { get; set; } = contactInfo;
    [JsonIgnore] public TimeSpan? NearestReturnDate { get; set; }
    [JsonIgnore] public string ReturnDateInfo { get; set; } = "Нет долгов"; // Для отображения
    [JsonIgnore] public int SortDays { get; set; } = int.MaxValue; // MaxValue = нет долгов


    public override string ToString() => $"{lastName} {firstName} {middleName}, {contactInfo}";
    [JsonIgnore] public string FullName => $"{lastName} {firstName} {middleName}";

}