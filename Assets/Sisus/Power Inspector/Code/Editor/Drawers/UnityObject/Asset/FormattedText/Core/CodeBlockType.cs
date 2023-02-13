namespace Sisus
{
	public enum CodeBlockType
	{
		Default,
		String,
		Char,
		CommentBlock,
		CommentLine,
		Type,       //List, MyClass
		Keyword,    //ref, class, string...
		Number,
		PreprocessorDirective,
		LineBreak,
		Unformatted
	}
}