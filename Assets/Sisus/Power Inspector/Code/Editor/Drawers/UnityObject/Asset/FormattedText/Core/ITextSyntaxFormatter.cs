namespace Sisus
{
	public interface ITextSyntaxFormatter
	{
		string TextUnformatted { get; }

		CodeBlockGroup GeneratedBlocks { get; }

		string CommentColorTag { get; }
		string NumberColorTag { get; }
		string StringColorTag { get; }
		string KeywordColorTag { get; }

		void SetCode(string value);
		void SetLine(int lineNumber, string value);
		void BuildAllBlocks();
		void Clear();
		void Dispose();
	}
}