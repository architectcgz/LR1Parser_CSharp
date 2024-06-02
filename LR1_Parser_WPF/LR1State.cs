namespace Exp3_test;

public class Lr1State
{
    public int StateIndex { get; }
    public HashSet<Lr1Item> Items { get; }

    public override string ToString()
    {
        string result = "当前状态的index: "+StateIndex;
        foreach (var lr1Item in Items)
        {
            result += "\n" + lr1Item;
        }

        result += $"\n当前状态的HashCode: {GetHashCode()}";
        return result;
    }

    public Lr1State(int stateIndex,HashSet<Lr1Item> items)
    {
        this.StateIndex = stateIndex;
        Items = new HashSet<Lr1Item>(items);
    }

    public override bool Equals(object obj)
    {
        return obj is Lr1State state &&
               Items.SetEquals(state.Items);
    }

    public override int GetHashCode()
    {
        int hashCode = 5381;
        foreach (var item in Items)
        {
            hashCode = (hashCode * 33) ^ item.GetHashCode();
        }
        return hashCode;
    }
}