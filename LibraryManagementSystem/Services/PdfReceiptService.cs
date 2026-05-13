using LibraryManagementSystem.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Drawing;
using QuestPDF.Elements;

namespace LibraryManagementSystem.Services
{
    public class PdfReceiptService
    {
        public PdfReceiptService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] GenerateIssueReceipt(BorrowRecord record)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    // PAGE SIZE
                    page.Size(PageSizes.A4);

                    // MARGIN
                    page.Margin(30);

                    // BACKGROUND
                    page.PageColor(Colors.White);

                    // DEFAULT FONT
                    page.DefaultTextStyle(x =>
                        x.FontSize(12)
                         .FontColor(Colors.Grey.Darken4));

                    // ================= HEADER =================

                    page.Header().Container().PaddingBottom(20).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("LIBRARY MANAGEMENT SYSTEM")
                                .FontSize(24)
                                .Bold()
                                .FontColor("#2563EB");

                            col.Item().Text("Book Issue / Return Receipt")
                                .FontSize(13)
                                .FontColor(Colors.Grey.Darken1);

                            col.Item().PaddingTop(5).Text($"Generated: {DateTime.Now:dd MMM yyyy hh:mm tt}")
                                .FontSize(10)
                                .FontColor(Colors.Grey.Medium);
                        });

                        row.ConstantItem(100).Height(60).AlignRight().AlignMiddle().Text("📚")
                            .FontSize(40);
                    });

                    // ================= CONTENT =================

                    page.Content().Column(col =>
                    {
                        // TOP CARD
                        col.Item().Container()
                            .Background("#F8FAFC")
                            .Border(1)
                            .BorderColor("#E2E8F0")
                            .Padding(20)
                            .Column(card =>
                            {
                                card.Spacing(12);

                                card.Item().Text("Borrow Information")
                                    .FontSize(18)
                                    .Bold()
                                    .FontColor("#0F172A");

                                card.Item().LineHorizontal(1)
                                    .LineColor("#CBD5E1");

                                // BOOK
                                card.Item().Row(r =>
                                {
                                    r.ConstantItem(160)
                                        .Text("Book Title")
                                        .Bold();

                                    r.RelativeItem()
                                        .Text(record.Book?.Title ?? "-");
                                });

                                // MEMBER
                                card.Item().Row(r =>
                                {
                                    r.ConstantItem(160)
                                        .Text("Member Name")
                                        .Bold();

                                    r.RelativeItem()
                                        .Text(record.Member?.Name ?? "-");
                                });

                                // ISBN
                                card.Item().Row(r =>
                                {
                                    r.ConstantItem(160)
                                        .Text("ISBN")
                                        .Bold();

                                    r.RelativeItem()
                                        .Text(record.Book?.ISBN ?? "-");
                                });

                                // ISSUE DATE
                                card.Item().Row(r =>
                                {
                                    r.ConstantItem(160)
                                        .Text("Issued On")
                                        .Bold();

                                    r.RelativeItem()
                                        .Text(record.IssuedOn.ToString("dd MMM yyyy"));
                                });

                                // DUE DATE
                                card.Item().Row(r =>
                                {
                                    r.ConstantItem(160)
                                        .Text("Due Date")
                                        .Bold();

                                    r.RelativeItem()
                                        .Text(record.DueDate.ToString("dd MMM yyyy"));
                                });

                                // RETURN DATE
                                card.Item().Row(r =>
                                {
                                    r.ConstantItem(160)
                                        .Text("Returned On")
                                        .Bold();

                                    r.RelativeItem()
                                        .Text(
                                            record.ReturnedOn != null
                                            ? record.ReturnedOn.Value.ToString("dd MMM yyyy")
                                            : "Not Returned"
                                        );
                                });

                                // STATUS
                                card.Item().Row(r =>
                                {
                                    r.ConstantItem(160)
                                        .Text("Status")
                                        .Bold();

                                    r.RelativeItem().Text(text =>
                                    {
                                        if (record.Status == "Returned")
                                        {
                                            text.Span("RETURNED")
                                                .Bold()
                                                .FontColor(Colors.Green.Darken2);
                                        }
                                        else
                                        {
                                            text.Span("ISSUED")
                                                .Bold()
                                                .FontColor(Colors.Orange.Darken2);
                                        }
                                    });
                                });

                                // FINE
                                card.Item().Row(r =>
                                {
                                    r.ConstantItem(160)
                                        .Text("Fine Amount")
                                        .Bold();

                                    r.RelativeItem().Text(text =>
                                    {
                                        text.Span($"₹ {record.FineAmount}")
                                            .Bold()
                                            .FontColor(
                                                record.FineAmount > 0
                                                ? Colors.Red.Darken2
                                                : Colors.Green.Darken2
                                            );
                                    });
                                });
                            });

                        // SPACE
                        col.Item().PaddingVertical(20);

                        // NOTE
                        col.Item().Container()
                            .Background("#EFF6FF")
                            .Border(1)
                            .BorderColor("#BFDBFE")
                            .Padding(15)
                            .Text(text =>
                            {
                                text.Span("Important: ")
                                    .Bold()
                                    .FontColor("#1D4ED8");

                                text.Span("Please return books before the due date to avoid late fines.");
                            });

                        // SIGNATURE
                        col.Item().PaddingTop(50).Row(r =>
                        {
                            r.RelativeItem();

                            r.ConstantItem(220).Column(signature =>
                            {
                                signature.Item()
                                    .LineHorizontal(1);

                                signature.Item()
                                    .AlignCenter()
                                    .Text("Authorized Signature")
                                    .FontSize(11)
                                    .FontColor(Colors.Grey.Darken1);
                            });
                        });
                    });

                    // ================= FOOTER =================

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Library Management System")
                            .Bold();

                        text.Span("  •  ");

                        text.Span("Premium Receipt");
                    });
                });
            }).GeneratePdf();
        }
    }
}