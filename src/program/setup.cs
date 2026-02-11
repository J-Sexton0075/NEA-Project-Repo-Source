using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Setup {

    public static class SetupMain {

        // SETUP CONSTANTS
        // NOTE: this was adjusted to use Path.GetFullPath rather than a debug path, and than later adjusted to just use a relative path to prevent future issues. 
        // This still requires adjustment to get to work with the debugger. 
        // public static string managerPath = "userresources/CalendarManager.json"; // The path to the calendar manager file
        public static string managerPath = Path.GetFullPath("../../../userresources/CalendarManager.json"); // The path to the calendar manager file while using the debugger. 
        // ^ Can be accessed across the program. 
        // nts You might want to consider moving it to management.cs later to keep these sorts of values in the same place?

    

        public static void InitialiseApplication() {
            // Check if the user has the default management file. 
            if (!File.Exists(managerPath)) {
                CreateInitial();
            }
            LoadInitial();

        }

        public static void CreateInitial() { // Creates a new management file
            Manager.CalendarManager managerObj = new Manager.CalendarManager("CalendarManager", new Dictionary<string, string> { });
            string toJson = JsonConvert.SerializeObject(managerObj);
            File.WriteAllText(managerPath, toJson); // Write json to new file 
        }


        public static void LoadInitial() {
            string toObj = File.ReadAllText(managerPath);
            Manager.ManagerValues.RootManager = JsonConvert.DeserializeObject<Manager.CalendarManager>(toObj);
        } 
    }
}