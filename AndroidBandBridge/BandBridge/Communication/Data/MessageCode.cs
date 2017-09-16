namespace Communication.Data
{
    /// <summary>
    /// Message commands type codes.
    /// </summary>
    public enum MessageCode : byte
    {
        SHOW_LIST_ASK, SHOW_LIST_ANS,   // request for synchronizing connected Bands list
        GET_DATA_ASK, GET_DATA_ANS,     // request for current sensors data from specific Band device
        CALIB_ASK, CALIB_ANS,           // request for calibrating sensors data from specific Band device
        CTR_MSG,                        // control message
    }
}