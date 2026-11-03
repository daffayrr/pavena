using FluentAssertions;
using Pavena.Application.Documents.DTOs;
using Pavena.Application.Documents.Validators;
using Pavena.Domain.Enums;
using Xunit;

namespace Pavena.UnitTests.Validators;

public class DocumentValidatorTests
{
    [Fact]
    public async Task CreateDocumentRequestValidator_ValidatesTitleLength()
    {
        var validator = new CreateDocumentRequestValidator();

        var emptyTitleReq = new CreateDocumentRequest("", "Desc", "Manuscript", EditorMode.Latex, null);
        var res1 = await validator.ValidateAsync(emptyTitleReq);
        res1.IsValid.Should().BeFalse();
        res1.Errors.Should().Contain(e => e.PropertyName == "Title");

        var longTitleReq = new CreateDocumentRequest(new string('A', 251), "Desc", "Manuscript", EditorMode.Latex, null);
        var res2 = await validator.ValidateAsync(longTitleReq);
        res2.IsValid.Should().BeFalse();

        var validReq = new CreateDocumentRequest("Valid Title", "Desc", "Manuscript", EditorMode.Latex, null);
        var res3 = await validator.ValidateAsync(validReq);
        res3.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CreateFileRequestValidator_RejectsDirectoryTraversalAndIllegalChars()
    {
        var validator = new CreateFileRequestValidator();

        var traversalReq = new CreateFileRequest("../secret.key", "content");
        var res1 = await validator.ValidateAsync(traversalReq);
        res1.IsValid.Should().BeFalse();
        res1.Errors.Should().Contain(e => e.ErrorMessage.Contains("traversal"));

        var illegalCharReq = new CreateFileRequest("file<name>.tex", "content");
        var res2 = await validator.ValidateAsync(illegalCharReq);
        res2.IsValid.Should().BeFalse();
        res2.Errors.Should().Contain(e => e.ErrorMessage.Contains("illegal"));

        var validReq = new CreateFileRequest("sections/intro.tex", "\\section{Introduction}");
        var res3 = await validator.ValidateAsync(validReq);
        res3.IsValid.Should().BeTrue();
    }
}
