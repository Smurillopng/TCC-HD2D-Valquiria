namespace Sisus
{
	/// <summary> A plain text syntax formatter. This class cannot be inherited. </summary>
	public sealed class PlainTextSyntaxFormatter : SyntaxFormatterBase<PlainTextSyntaxFormatter>
	{
		/// <inheritdoc/>
		protected override bool IsCommentBlockStart()
		{
			return false;
		}

		/// <inheritdoc/>
		protected override bool IsCommentBlockEnd()
		{
			return false;
		}

		/// <inheritdoc/>
		protected override bool IsCommentLineStart()
		{
			return false;
		}

		/// <inheritdoc/>
		protected override bool IsStringStart()
		{
			return false;
		}

		/// <inheritdoc/>
		protected override bool IsCharStart()
		{
			return false;
		}

		/// <inheritdoc/>
		protected override bool IsPreprocessorDirectiveStart()
		{
			return false;
		}

		/// <inheritdoc/>
		protected override int IsType()
		{
			return -1;
		}

		/// <inheritdoc/>
		protected override int IsKeyword()
		{
			return -1;
		}

		/// <inheritdoc/>
		protected override bool IsNumber()
		{
			return false;
		}

		/// <inheritdoc/>
		public override void Dispose()
		{
			var disposing = this;
			disposing.Clear();
			PlainTextSyntaxFormatterPool.Dispose(ref disposing);
		}
	}
}