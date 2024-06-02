namespace Exp3_test;
/// <summary>
/// LR1分析中的一个项目
/// </summary>
/// 
public class Lr1Item
{
    public string Left { get;} // 左部
    public string Right { get;} // 右部
    public int DotPosition { get; } // 点的位置
    public HashSet<char> Lookahead { get;} // 搜索符号

    public Lr1Item(string left, string right, int dotPosition, HashSet<char> lookahead)
    {
        this.Left = left;
        this.Right = right;
        DotPosition = dotPosition;
        Lookahead = new HashSet<char>(lookahead);
    }

    public override string ToString()
    {
        string grammar = Left + "->" + Right.Insert(DotPosition,".");
        grammar += ",";
        foreach (var c in Lookahead)
        {
            grammar += c + "|";
        }
        return "[" + grammar + "]";
    }

    public override bool Equals(object obj)
    {
        if (obj is Lr1Item item)
        {
            return Left == item.Left &&
                   Right == item.Right &&
                   DotPosition == item.DotPosition &&
                   Lookahead.SetEquals(item.Lookahead);
        }
        return false;
    }


    public override int GetHashCode()
    {
        int hashCode = -1951714242;
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Left);
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Right);
        hashCode = hashCode * -1521134295 + DotPosition.GetHashCode();
        foreach (var la in Lookahead)
        {
            hashCode = hashCode * -1521134295 + la.GetHashCode();
        }
        return hashCode;
    }

}