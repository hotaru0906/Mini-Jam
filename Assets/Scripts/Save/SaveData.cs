using System;
using System.Collections.Generic;

[Serializable]
public class SaveChoiceRecord
{
    public string eventID;
    public int selectedChoiceIndex;
}

[Serializable]
public class SaveData
{
    public List<string> completedEventIDs = new List<string>();
    public List<string> unlockedKnowledgeIDs = new List<string>();
    public List<SaveChoiceRecord> choiceRecords = new List<SaveChoiceRecord>();
}
