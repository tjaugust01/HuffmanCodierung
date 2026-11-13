namespace HuffmanCodierung;

public class Node
{
    public int Freq;
    public byte? ByteVal;
    public Node? Left;
    public Node? Right;

    public bool IsLeaf => Left == null && Right == null;
    
    public Node(
        int freq,
        byte? byteVal = null,
        Node? left = null,
        Node? right = null)
    {
        Freq = freq;
        ByteVal = byteVal;
        Left = left;
        Right = right;
    }
}