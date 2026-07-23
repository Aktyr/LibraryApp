namespace LibApp.Application.Commands.Entities.Books;

public class DeleteBookCommand(IUnitOfWork unitOfWork)
    : IDeleteCommand<DeleteBookRequest, BasicCreateDeleteResponse>, ICommand
{
    public async Task<BasicCreateDeleteResponse> Execute(DeleteBookRequest request, CancellationToken cancellationToken)
    {
        var bookRepo = unitOfWork.GetRepository<Book>();
        var books = await bookRepo.Get(x => x.Id.Value == request.Id.Value, cancellationToken);
        var book = books.FirstOrDefault() ?? throw new BookNotFoundException();

        await bookRepo.Remove(book, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ResponseFactory.Deleted<Book>();
    }
}