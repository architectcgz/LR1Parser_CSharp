namespace Exp3_test;

/// <summary>
/// 文法的定义
/// </summary>
public class Grammar
{
    public string Left;//左部
    public string Right;//右部
    public int Index;//产生式的序号

    public Grammar(string left, string right, int index)
    {
        this.Left = left;
        this.Right = right;
        this.Index = index;
    }

    public override string ToString()
    {
        return Left + "->" + Right + "推导序号:" + Index;
    }
}