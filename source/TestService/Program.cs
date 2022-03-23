// See https://aka.ms/new-console-template for more information

using Tests;

Console.WriteLine("Hello, World!");
FacebookTest myTest = new FacebookTest();

myTest.TestFbManagerSendVideoMetaDataTo4GroupsAsync();
Console.WriteLine("Tests done");
Console.ReadLine();



#pragma warning disable IDE0059 // Unnecessary assignment of a value
// ReSharper disable once InconsistentNaming
var Elki = new Gauppi();
var Oli = new Gauppi();

// ReSharper disable once InconsistentNaming
var Jakob = new Gauppi
            {
                FirstName = "Jakob",
                Height = 53,
                Weight = 4110,
                DateOfBirth = new DateTime(2022,02,15,14,54,00)
            };
Jakob.SetToVeryAlive();
Elki.SetToVeryLucky();
Oli.SetToVeryLucky();
Elki.StartRefurbishment();

#pragma warning restore IDE0059 // Unnecessary assignment of a value


public class Gauppi
{
    public string FirstName;
    public short Height;
    public short Weight;
    public DateTime DateOfBirth;

    public void StartRefurbishment()
    {}

    public void SetToVeryLucky()
    {}

    public void SetToVeryAlive()
    {}
}