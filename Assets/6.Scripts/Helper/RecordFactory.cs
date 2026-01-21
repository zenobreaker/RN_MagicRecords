using UnityEngine;

public static class RecordFactory
{
    public static RecordPassive CreateRecordPassive(SO_RecordData data)
    {
        if (data == null) return null; 
        if( string.IsNullOrEmpty(data.className))
            return new RecordPassive(data);

        return data.className switch
        {
            "Record_AutoReload" => new Record_AutoReload(data),
            _ => new RecordPassive(data)
        }; 
    }
}