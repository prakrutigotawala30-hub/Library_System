using ClosedXML.Excel;
using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.Services
{
    public class ExportService
    {
        // ================= BOOKS EXPORT =================
        public byte[] ExportBooks(List<Book> books)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Books");

            // Headers (FIXED)
            ws.Cell(1, 1).Value = "Id";
            ws.Cell(1, 2).Value = "Title";
            ws.Cell(1, 3).Value = "Author";
            ws.Cell(1, 4).Value = "Category";
            ws.Cell(1, 5).Value = "Total Copies";
            ws.Cell(1, 6).Value = "Available Copies";

            int row = 2;

            foreach (var b in books)
            {
                ws.Cell(row, 1).Value = b.Id;
                ws.Cell(row, 2).Value = b.Title;

                // IMPORTANT FIX (null-safe string)
                ws.Cell(row, 3).Value = b.Author != null ? b.Author.Name : "";
                ws.Cell(row, 4).Value = b.Category != null ? b.Category.Name : "";

                ws.Cell(row, 5).Value = b.TotalCopies;
                ws.Cell(row, 6).Value = b.AvailableCopies;

                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        // ================= MEMBERS EXPORT =================
        public byte[] ExportMembers(List<Member> members)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Members");

            ws.Cell(1, 1).Value = "Id";
            ws.Cell(1, 2).Value = "Name";
            ws.Cell(1, 3).Value = "Email";
            ws.Cell(1, 4).Value = "Phone";

            int row = 2;
            foreach (var m in members)
            {
                ws.Cell(row, 1).Value = m.Id;
                ws.Cell(row, 2).Value = m.Name;
                ws.Cell(row, 3).Value = m.Email;
                ws.Cell(row, 4).Value = m.Phone;
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        // ================= BORROW EXPORT =================
        public byte[] ExportBorrows(List<BorrowRecord> borrows)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("BorrowRecords");

            ws.Cell(1, 1).Value = "Id";
            ws.Cell(1, 2).Value = "Book";
            ws.Cell(1, 3).Value = "Member";
            ws.Cell(1, 4).Value = "Issue Date";
            ws.Cell(1, 5).Value = "Return Date";
            ws.Cell(1, 6).Value = "Fine";

            int row = 2;
            foreach (var b in borrows)
            {
                ws.Cell(row, 1).Value = b.Id;
                ws.Cell(row, 2).Value = b.Book?.Title;
                ws.Cell(row, 3).Value = b.Member?.Name;
                ws.Cell(row, 4).Value = b.IssuedOn.ToString("dd-MM-yyyy");
                ws.Cell(row, 5).Value = b.ReturnedOn?.ToString("dd-MM-yyyy");
                ws.Cell(row, 6).Value = b.FineAmount;
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}