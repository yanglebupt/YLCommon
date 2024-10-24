
using YLCommon;

class LoggerTest
{
    public void Print()
    {
        this.Log("Log {0}-{1}", "Mike", 10);
        this.ColorLog(LogColor.Cyan, "ColorLog Cyan {0}-{1}", "Mike", 10);
        this.ColorLog(LogColor.Magenta, "ColorLog Magenta {0}-{1}", "Mike", 10);
        this.Warn("Warn {0}-{1}", "Mike", 10);
        this.Error("Error {0}-{1}", "Jack", 10);
    }
};
