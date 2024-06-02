using System.IO;
using Exp3_WPF;

namespace Exp3_test;

public class LR1Parser
{
    //预制的终结符表，用于初步判断文法中的那些是终结符
    //实际上文法的终结符经过Preprocess函数处理后放在vt中
    private static readonly List<char> VtPremade = new()
    {
        '+', '-', '*', '/', '(', ')', '=',
        'a', 'b', 'c', 'd', 'e',
        'f', 'g', 'h', 'i', 'j',
        'k', 'l', 'm', 'n', 'o',
        'p', 'q', 'r', 's', 't',
        'u', 'v', 'w', 'x', 'y', 'z'
    };

    private string _startWord;
    private int _startIndex;
    private bool _initialized;
    private List<string> _grammarLines;
    private List<Grammar> _grammar;
    private HashSet<char> Vt { get; set; }
    private HashSet<string> Vn { get; set; }
    private Dictionary<string, HashSet<char>> First { get; set; }
    private List<Lr1State> _canonicalCollection;
    private Dictionary<(HashSet<Lr1Item>, char), HashSet<Lr1Item>> _gotoCache;
    private Dictionary<(int, char), string> _actionTable;
    private Dictionary<(int, string), int> _gotoTable;
    private List<StepInfo> _stepInfos;

    public List<StepInfo> StepInfos => _stepInfos;
    public Dictionary<(int, char), string> ActionTable => _actionTable;
    public Dictionary<(int, string), int> GotoTable => _gotoTable;

    public LR1Parser()
    {
        _grammarLines = new();
        _stepInfos = new();
        _startWord = "";
        _gotoCache = new();
        Vt = new();
        Vn = new();
        First = new();
        _grammar = new();
        _canonicalCollection = new List<Lr1State>();
        _actionTable = new Dictionary<(int, char), string>();
        _gotoTable = new Dictionary<(int, string), int>();
    }

    public void PrintCanonicalCollection()
    {
        foreach (var lr1State in _canonicalCollection)
        {
            Console.WriteLine(lr1State.ToString());
        }
    }

    public void PrintVtAndVn()
    {
        if (!_initialized)
        {
            Console.WriteLine("请先读入文法并进行预处理!");
            return;
        }

        Console.Write("Vt:");
        foreach (var c in Vt)
        {
            Console.Write(c + ",");
        }

        Console.WriteLine();
        Console.Write("Vn:");
        foreach (var se in Vn)
        {
            Console.Write(se + ",");
        }
    }

    public bool Initialized => _initialized;
    public List<Grammar> Grammar => _grammar;

    /// <summary>
    /// 从文件中读出文法
    /// </summary>
    /// <param name="filePath"></param>
    public void ReadGrammarFromFile(string filePath)
    {
        try
        {
            _grammarLines = File.ReadLines(filePath).Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public void PrintGrammar()
    {
        if (!_initialized)
        {
            Console.WriteLine("请先预处理文法，进行初始化");
            return;
        }

        foreach (var grammar1 in _grammar)
        {
            Console.WriteLine(grammar1.Left + "->" + grammar1.Right + " 编号: " + grammar1.Index);
        }
    }

    //检查是否需要拓广
    private bool CheckNeedAugment()
    {
        bool need = false;
        var firstIndex = _grammarLines[0].IndexOf("->");
        _startWord = _grammarLines[0].Substring(0, firstIndex);
        for (int i = 1; i < _grammarLines.Count; i++)
        {
            var index = _grammarLines[i].IndexOf("->");
            var left = _grammarLines[i].Substring(0, index);
            if (left == _startWord)
            {
                need = true;
                break;
            }
        }

        return need;
    }

    //对文法进行拓广
    private void Augment()
    {
        string augmentedStartWord = _startWord + "'";
        string augmentedProduction = augmentedStartWord + "->" + _startWord;
        _grammarLines.Insert(0, augmentedProduction);
        _startWord = augmentedStartWord;
        _startIndex = 1;
    }

    private void AddToVtOrVn(string a)
    {
        if (VtPremade.Contains(a[0]))
        {
            Vt.Add(a[0]);
        }
        else if (a[0] != '$')
        {
            Vn.Add(a);
        }
    }

    /// <summary>
    /// 对文法进行预处理,将文法转化为Grammar类
    /// </summary>
    public void PreprocessGrammar()
    {
        if (_grammarLines.Count == 0)
        {
            Console.WriteLine("文法的数量为0,请先输入文法!");
            return;
        }

        if (CheckNeedAugment())
        {
            Augment();
        }

        int wordIndex = _startIndex;
        try
        {
            foreach (var grammarLine in _grammarLines)
            {
                var index = grammarLine.IndexOf("->");
                if (index == -1)
                {
                    Console.WriteLine("文法格式错误,请修改");
                    return;
                }

                var left = grammarLine.Substring(0, index);
                var right = grammarLine.Substring(index + 2);
                Vn.Add(left);
                var rightWords = right.Split("|").ToList();
                foreach (var rightWord in rightWords)
                {
                    for (int i = 0; i < rightWord.Length; i++)
                    {
                        string w;
                        if (i + 1 < rightWord.Length)
                        {
                            if (rightWord[i + 1] == '\'')
                            {
                                w = rightWord.Substring(i, 2);
                                i++;
                            }
                            else
                            {
                                w = rightWord[i].ToString();
                            }
                        }
                        else
                        {
                            w = rightWord[i].ToString();
                        }

                        AddToVtOrVn(w);
                    }

                    _grammar.Add(new Grammar(left, rightWord, wordIndex));
                    wordIndex++;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        PrintGrammar();
        _initialized = true;
    }

    /// <summary>
    /// 求First集合的包装器
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    private HashSet<char> GetOneFirstWrapper(string c)
    {
        return GetOneFirst(c, new HashSet<string>());
    }

    private HashSet<char> GetOneFirst(string c, HashSet<string> currentlyProcessing)
    {
        if (First.ContainsKey(c))
        {
            return First[c];
        }

        if (currentlyProcessing.Contains(c))
        {
            return new HashSet<char>();
        }

        currentlyProcessing.Add(c);

        HashSet<char> oneFirst = new();
        foreach (var item in _grammar)
        {
            for (int i = 0; i < item.Right.Length; i++)
            {
                if (i + 1 < item.Right.Length)
                {
                    string symbol = item.Right[i + 1] == '\'' ? item.Right.Substring(i, 2) : item.Right[i].ToString();
                    i += symbol.Length - 1;

                    if (Vt.Contains(symbol[0]))
                    {
                        oneFirst.Add(symbol[0]);
                        break;
                    }

                    if (Vn.Contains(symbol))
                    {
                        var nextFirst = GetOneFirst(symbol, currentlyProcessing);
                        foreach (var symbolFirst in nextFirst)
                        {
                            if (symbolFirst != '$')
                            {
                                oneFirst.Add(symbolFirst);
                            }
                        }

                        if (!nextFirst.Contains('$'))
                        {
                            break;
                        }
                    }
                    else if (symbol[0] == '$')
                    {
                        oneFirst.Add('$');
                        break;
                    }
                }
                else
                {
                    char symbol = item.Right[i];
                    if (Vt.Contains(symbol))
                    {
                        oneFirst.Add(symbol);
                        break;
                    }

                    if (Vn.Contains(symbol.ToString()))
                    {
                        var nextFirst = GetOneFirst(symbol.ToString(), currentlyProcessing);
                        foreach (var symbolFirst in nextFirst)
                        {
                            if (symbolFirst != '$')
                            {
                                oneFirst.Add(symbolFirst);
                            }
                        }

                        if (!nextFirst.Contains('$'))
                        {
                            break;
                        }
                    }
                    else if (symbol == '$')
                    {
                        oneFirst.Add('$');
                        break;
                    }
                }
            }
        }

        First[c] = oneFirst;
        return oneFirst;
    }

    public HashSet<Lr1Item> Closure(HashSet<Lr1Item> items)
    {
        // 使用新的集合来存储闭包结果，避免修改原始集合
        var closureSet = new HashSet<Lr1Item>(items);
        bool added = true;

        while (added)
        {
            added = false;

            var newItems = new HashSet<Lr1Item>();

            foreach (var item in closureSet)
            {
                string right = item.Right;
                int dotPosition = item.DotPosition;

                if (dotPosition < right.Length)
                {
                    char symbol = right[dotPosition];

                    // 如果点在非终结符之前，计算它的搜索符
                    if (Vn.Contains(symbol.ToString()))
                    {
                        var lookaheadSet = new HashSet<char>();

                        // α.Bβ  β不为空的情况
                        if (dotPosition + 1 < right.Length)
                        {
                            char nextSymbol = right[dotPosition + 1];

                            // 如果下一个符号是终结符，则直接加入搜索符集合
                            if (Vt.Contains(nextSymbol))
                            {
                                lookaheadSet.Add(nextSymbol);
                            }
                            // 如果下一个符号是非终结符，则加入其FIRST集合
                            else
                            {
                                lookaheadSet.UnionWith(First[nextSymbol.ToString()]);
                            }
                        }
                        // β为空的情况，继承当前项目的搜索符
                        else
                        {
                            lookaheadSet.UnionWith(item.Lookahead);
                        }

                        // 遍历产生式并生成新的LR1项目
                        foreach (var production in _grammar)
                        {
                            // 文法的左部必须与点后面的元素相同
                            if (production.Left == symbol.ToString())
                            {
                                var newItem = new Lr1Item(symbol.ToString(), production.Right, 0, lookaheadSet);

                                // 查找closureSet中是否已经有相同项目
                                var existingItem = FindMatchingItem(closureSet, newItem);

                                if (existingItem != null)
                                {
                                    // 合并Lookahead
                                    int previousCount = existingItem.Lookahead.Count;
                                    existingItem.Lookahead.UnionWith(lookaheadSet);
                                    // 如果有新的lookahead加入则设置added为true
                                    if (existingItem.Lookahead.Count > previousCount)
                                    {
                                        added = true;
                                    }
                                }
                                else
                                {
                                    newItems.Add(newItem);
                                    added = true;
                                }
                            }
                        }
                    }
                }
            }

            // 将新项目添加到closureSet
            closureSet.UnionWith(newItems);
        }

        return closureSet;
    }

    // 辅助方法：查找closureSet中是否有匹配的LR1Item
    private Lr1Item FindMatchingItem(HashSet<Lr1Item> closureSet, Lr1Item newItem)
    {
        foreach (var item in closureSet)
        {
            if (item.Left == newItem.Left && item.Right == newItem.Right && item.DotPosition == newItem.DotPosition)
            {
                return item;
            }
        }

        return null;
    }


    /// <summary>
    /// 计算应用一个符号的给定状态能达到的状态
    /// 如S->.L=R 此时的symbol为L
    /// 那么经过L得到 S->L.=R
    /// 左部右部均不变，dot的位置后移一位
    /// </summary>
    /// <param name="items"></param>
    /// <param name="symbol"></param>
    /// <returns></returns>
    private HashSet<Lr1Item> Goto(HashSet<Lr1Item> items, char symbol)
    {
        var key = (items, symbol);

        // 检查缓存中是否已有结果
        if (_gotoCache.TryGetValue(key, out var cachedResult))
        {
            //Console.WriteLine("从Cache中取出结果");
            return cachedResult;
        }

        var gotoSet = new HashSet<Lr1Item>();

        foreach (var item in items)
        {
            if (item.DotPosition < item.Right.Length && item.Right[item.DotPosition] == symbol)
            {
                gotoSet.Add(new Lr1Item(item.Left, item.Right, item.DotPosition + 1, item.Lookahead));
            }
        }

        var gotoResult = Closure(gotoSet);

        // 将结果存入缓存
        _gotoCache[key] = gotoResult;

        return gotoResult;
    }



    public void BuildCanonicalCollection()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("尚未初始化!");
        }

        // 开始的第一个I0元素
        var startItems = new HashSet<Lr1Item>
        {
            new Lr1Item(_startWord, _grammar[0].Right, 0, ['#'])
        };
        // 开始时的状态集合
        _canonicalCollection.Add(new Lr1State(0, Closure(startItems)));

        int stateIndex = 1;
        bool added = true;
        while (added)
        {
            var stateSet = new List<Lr1State>();
            added = false;
            foreach (var state in _canonicalCollection)
            {
                var transformSymbols = new HashSet<char>();
                foreach (var stateItem in state.Items)
                {
                    int dotPos = stateItem.DotPosition;
                    // 点的位置不在末尾时，点后面的单词就是转换元素
                    if (dotPos < stateItem.Right.Length)
                    {
                        transformSymbols.Add(stateItem.Right[dotPos]);
                    }
                }

                foreach (var symbol in transformSymbols)
                {
                    var gotoResult = Goto(state.Items, symbol);
                    if (gotoResult.Count > 0)
                    {
                        //如果状态集合中没有Goto求出的新的状态集合，就将Goto求出的状态集合加入
                        var isStateExist = _canonicalCollection.Any(
                            s => s.Items.All(
                                i => gotoResult.Any(
                                    g => g.Equals(i))
                            ) && s.Items.Count == gotoResult.Count
                        );

                        if (!isStateExist)
                        {
                            var newState = new Lr1State(stateIndex, gotoResult);
                            stateSet.Add(newState);
                            stateIndex++;
                            added = true;
                        }
                    }
                }
            }

            _canonicalCollection.AddRange(stateSet);
        }
    }

    /// <summary>
    /// 由LR(1)项目集合规范族求出LR(1)分析表
    /// </summary>
    public void GenerateAnalysisTable()
    {
        for (int i = 0; i < _canonicalCollection.Count; i++)
        {
            var items = _canonicalCollection[i].Items;
            foreach (var item in items)
            {
                var dotPos = item.DotPosition;
                //.在末尾,说明是规约项目
                if (dotPos == item.Right.Length)
                {
                    //查找对应的推导式的编号
                    var gram = _grammar.Find(e => e.Left.Equals(item.Left) && e.Right.Equals(item.Right));
                    //添加到action
                    if (gram != null)
                    {
                        if (gram.Left == _startWord)
                        {
                            foreach (var c in item.Lookahead)
                            {
                                Console.WriteLine($"从状态{_canonicalCollection[i].StateIndex}经过{c}转移到了状态ACC");
                                _actionTable[(_canonicalCollection[i].StateIndex, c)] = "ACC";
                            }

                        }
                        else
                        {
                            foreach (var c in item.Lookahead)
                            {
                                Console.WriteLine($"从状态{_canonicalCollection[i].StateIndex}经过{c}转移到了状态{gram.Index}");
                                _actionTable[(_canonicalCollection[i].StateIndex, c)] = "r" + gram.Index;
                            }
                        }
                    }

                }
                //.不在末尾，是移进项目
                else
                {
                    //拿到.后的单词
                    var word = item.Right[dotPos];
                    //.后的单词是非终结符，填写Goto表
                    if (Vn.Contains(word.ToString()))
                    {
                        var gotoResult = Goto(items, word);
                        //Goto集合有结果，说明有跳转项
                        if (gotoResult.Count > 0)
                        {
                            //从以及得到的规范族中找到跳转到的状态
                            //注意这里是比较内容而不是比较引用，所以用equals而不是==
                            var indexInCo = _canonicalCollection.Find(
                                e => e.Items.All(
                                    //GotoResult中任何一个值都要在CanonicalCollection中
                                    f => gotoResult.Any(
                                        g =>
                                            f.Left == g.Left &&
                                            f.Right == g.Right &&
                                            f.DotPosition == g.DotPosition &&
                                            f.Lookahead.SetEquals(g.Lookahead)
                                    )
                                )
                            );
                            if (indexInCo != null)
                            {
                                Console.WriteLine(
                                    $"从状态{_canonicalCollection[i].StateIndex}经过{word}转移到了状态{indexInCo.StateIndex}");
                                //添加到Goto表
                                _gotoTable[(_canonicalCollection[i].StateIndex, word.ToString())] =
                                    indexInCo.StateIndex;
                            }
                            else
                            {
                                Console.WriteLine(
                                    $"没找到从状态(StateIndex: {_canonicalCollection[i].StateIndex},HashCode:{_canonicalCollection[i].GetHashCode()}):");
                                foreach (var lr1Item in items)
                                {
                                    Console.WriteLine(lr1Item.ToString());
                                }

                                Console.WriteLine($"经过{word}到状态(HashCode{gotoResult.GetHashCode()}:");
                                foreach (var lr1Item in gotoResult)
                                {
                                    Console.WriteLine(lr1Item.ToString());
                                }

                                Console.WriteLine("的转换");
                            }
                        }
                    }
                    //右部是终结符，进行规约，填写S
                    else if (Vt.Contains(word))
                    {
                        var gotoResult = Goto(items, word);
                        if (gotoResult.Count > 0)
                        {
                            //从以及得到的规范族中找到跳转到的状态
                            var indexInCo = _canonicalCollection.Find(
                                //GotoResult中任何一个值都要在CanonicalCollection中
                                e => e.Items.All(
                                    f => gotoResult.Any(
                                        g =>
                                            f.Left == g.Left &&
                                            f.Right == g.Right &&
                                            f.DotPosition == g.DotPosition &&
                                            f.Lookahead.SetEquals(g.Lookahead)
                                    )
                                )
                            );


                            if (indexInCo != null)
                            {
                                Console.WriteLine(
                                    $"从状态{_canonicalCollection[i].StateIndex}经过{word}转移到了状态{indexInCo.StateIndex}");
                                _actionTable[(_canonicalCollection[i].StateIndex, word)] = "S" + indexInCo.StateIndex;
                            }
                            else
                            {
                                Console.WriteLine(
                                    $"没找到从状态(StateIndex: {_canonicalCollection[i].StateIndex},HashCode:{_canonicalCollection[i].GetHashCode()}):");
                                foreach (var lr1Item in items)
                                {
                                    Console.WriteLine(lr1Item.ToString());
                                }

                                Console.WriteLine($"经过{word}到状态(HashCode{gotoResult.GetHashCode()}:");
                                foreach (var lr1Item in gotoResult)
                                {
                                    Console.WriteLine(lr1Item.ToString());
                                }

                                Console.WriteLine("的转换");
                            }
                        }
                    }
                }

            }
        }
    }

    public void PrintParsingTable()
    {
        // 打印 actionTable
        Console.WriteLine("Action Table:");
        foreach (var entry in _actionTable)
        {
            Console.WriteLine($"Key: ({entry.Key.Item1}, '{entry.Key.Item2}'), Value: {entry.Value}");
        }

        // 打印 gotoTable
        Console.WriteLine("\nGoto Table:");
        foreach (var entry in _gotoTable)
        {
            Console.WriteLine($"Key: ({entry.Key.Item1}, \"{entry.Key.Item2}\"), Value: {entry.Value}");
        }

    }

    public string ParseInput(string input)
    {
        Stack<int> stateStack = new();
        Stack<string> symbolStack = new();
        _stepInfos = new();
        stateStack.Push(0);
        symbolStack.Push("#");
        int inputIndex = 0;
        int analysisIndex = 1;
        int inputLength = input.Length;
        do
        {
            var stepInfo = new StepInfo();
            stepInfo.StepCount = analysisIndex;
            string stateInfo = "";
            foreach (var i in stateStack.Reverse())
            {
                stateInfo += i + ",";
            }

            stepInfo.StateStack = stateInfo;
            string symbolInfo = "";
            foreach (var se in symbolStack.Reverse())      
            {
                symbolInfo += se;
            }

            stepInfo.SymbolStack = symbolInfo;
            stepInfo.Input = input.Substring(inputIndex);

            //查表Action[state,anchor]
            string actionInfo = "";
            // 使用 TryGetValue 来查找 Action 表中的值
            if (!_actionTable.TryGetValue((stateStack.Peek(), input[inputIndex]), out var actionTableResult))
            {
                return $"ERROR: 分析表中未定义的情况: Action[{stateStack.Peek()}, {input[inputIndex]}]";
            }
            
            //S开头，anchor进符号,S后面跟着的数字进状态栈
            if (actionTableResult[0] == 'S')
            {
                var stateStackToPush = int.Parse(actionTableResult.Substring(1));
                stateStack.Push(stateStackToPush);
                actionInfo =
                    $"Action[{stateStack.Peek()},{input[inputIndex]}] = {actionTableResult}，状态{stateStackToPush}入栈";
                symbolStack.Push(input[inputIndex].ToString());
                inputIndex++;
                analysisIndex++;
            }
            //Action表中为ACC，即成功
            else if (actionTableResult == "ACC")
            {
                stepInfo.Action = "ACC, 分析成功!";
                _stepInfos.Add(stepInfo);
                analysisIndex++;
                return "Success: Accept, 分析成功!";
            }
            //r开头,用查到的规则进行规约
            else if (actionTableResult[0] == 'r')
            {
                var grammarIndex = int.Parse(actionTableResult.Substring(1));
                var gram = _grammar.Find(e => e.Index == grammarIndex);
                if (gram != null)
                {

                    symbolStack.Pop();
                    symbolStack.Push(gram.Left);
                    for (int i = 0; i < gram.Right.Length; i++)
                    {
                        stateStack.Pop();
                    }
                    //Goto,获取数字栈中应放的内容
                    var oldStateStackTop = stateStack.Peek();
                    var gotoResult = _gotoTable[(stateStack.Peek(), symbolStack.Peek())];
                    actionInfo =
                        $"Action[{oldStateStackTop},{input[inputIndex]}] = r{grammarIndex},{gram.Left}->{gram.Right} 归约,GOTO({stateStack.Peek()},{symbolStack.Peek()})={gotoResult} 入栈";
                    stateStack.Push(gotoResult);
                    analysisIndex++;
                }
                else
                {
                    return $"ERROR: 找不到序号为{grammarIndex}的产生式";
                }
            }

            stepInfo.Action = actionInfo;
            _stepInfos.Add(stepInfo);
        } while (inputIndex < inputLength);

        // 如果输入串被完全处理，但是没有接受状态，则分析失败
        return "ERROR:分析失败：无法接受输入串";
    }
}





