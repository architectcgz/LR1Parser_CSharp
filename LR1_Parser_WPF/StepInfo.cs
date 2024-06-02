namespace Exp3_WPF;

public class StepInfo
{
    public int StepCount { get; set; } 
    public string StateStack { get; set; } 
    public string  SymbolStack{ get; set; }
    public string Input { get; set; } 
    public string Action { get; set; }
    public override string ToString()
    {
        return $"步骤:{StepCount}\t状态栈:{StateStack}\t符号栈:{SymbolStack}\t 输入串:{Input}\t动作说明:{Action}";
    }
}