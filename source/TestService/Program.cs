// See https://aka.ms/new-console-template for more information

using Tests;

Console.WriteLine("Hello, World!");
FacebookTest myTest = new FacebookTest();

myTest.TestFbManagerSendVideoMetaDataTo4GroupsAsync();
Console.WriteLine("Tests done");
Console.ReadLine();