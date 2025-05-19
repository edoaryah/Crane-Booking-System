// [Services/Email/EmailTemplate.cs]
// Berisi template HTML untuk email notifikasi.
using System.Text;
using AspnetCoreMvcFull.Models;

namespace AspnetCoreMvcFull.Services
{
  public class EmailTemplate
  {
    private readonly string _baseUrl;

    public EmailTemplate(IHttpContextAccessor httpContextAccessor)
    {
      // Mendapatkan base URL dari request saat ini
      var request = httpContextAccessor.HttpContext?.Request;
      if (request != null)
      {
        _baseUrl = $"{request.Scheme}://{request.Host}";
      }
      else
      {
        _baseUrl = "http://localhost:5055"; // Default fallback
      }
    }

    public string BookingSubmittedTemplate(string name, Booking booking)
    {
      string detailUrl = $"{_baseUrl}/BookingHistory/Details/{booking.Id}";

      return $@"<!doctype html>
            <html lang=""en"">
              <head>
                <meta charset=""UTF-8"">
                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
              </head>
              <body style=""font-family: Arial, sans-serif; margin: 0; padding: 0; background-color: #f8f9fa;"">
                <table role=""presentation"" style=""width: 100%; background-color: #f8f9fa;"" cellpadding=""0"" cellspacing=""0"">
                  <tr>
                    <td align=""center"">
                      <table role=""presentation"" class=""card"" style=""border: 1px solid #e0e0e0; border-radius: 4px; max-width: 600px; margin: 20px auto; background-color: white; box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);"" cellpadding=""0"" cellspacing=""0"">
                        <tr>
                          <td style=""background-color: #71dd37; color: white; padding: 15px; font-size: 20px; text-align: center; border-radius: 4px 4px 0 0;"">
                            <strong>Notifikasi Booking Crane</strong>
                          </td>
                        </tr>
                        <tr>
                          <td style=""padding: 20px; color: #212529; font-size: 16px;"">
                            <p>Yth, Bapak/Ibu {name},</p>
                            <p>Pengajuan booking crane dengan nomor <strong>{booking.BookingNumber}</strong> untuk area <strong>{booking.Location}</strong> telah berhasil diajukan.</p>
                            <p>Detail Booking:</p>
                            <ul>
                              <li>Crane: <strong>{booking.Crane?.Code ?? "Unknown"}</strong></li>
                              <li>Tanggal Mulai: <strong>{booking.StartDate:dd/MM/yyyy}</strong></li>
                              <li>Tanggal Selesai: <strong>{booking.EndDate:dd/MM/yyyy}</strong></li>
                            </ul>
                            <p>Booking Anda saat ini sedang menunggu persetujuan dari manager. Anda dapat memantau status booking dengan login ke sistem, silahkan cek link berikut:</p>
                            <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" style=""margin: 0 auto;"">
                              <tr>
                                <td align=""center"" style=""border-radius: 4px;"" bgcolor=""#71dd37"">
                                  <a href=""{detailUrl}"" target=""_blank"" style=""font-size: 16px; font-family: Arial, sans-serif; color: #ffffff; text-decoration: none; padding: 10px 20px; border-radius: 4px; background-color: #71dd37; border: 1px solid #71dd37; display: inline-block;"">
                                    Lihat Detail Booking
                                  </a>
                                </td>
                              </tr>
                            </table>
                            <div style=""color: #6c757d; font-size: 14px; margin-top: 20px;"">
                              <p>Terima Kasih,<br>
                              Crane Booking System<br>
                              <em>Terkirim otomatis oleh sistem.</em></p>
                            </div>
                          </td>
                        </tr>
                      </table>
                    </td>
                  </tr>
                </table>
              </body>
            </html>";
    }

    public string ManagerApprovalTemplate(string name, Booking booking, string createdBy, string badgeNumber, int stage)
    {
      byte[] bytesBN = Encoding.UTF8.GetBytes(badgeNumber);
      string encodedBN = Convert.ToBase64String(bytesBN);

      // Convert stage to string before encoding to Base64
      byte[] bytesStage = Encoding.UTF8.GetBytes(stage.ToString());
      string encodedStage = Convert.ToBase64String(bytesStage);

      string approvalUrl = $"{_baseUrl}/Approval/Manager?document_number={booking.Id}&badge_number={encodedBN}&stage={encodedStage}";

      return $@"<!doctype html>
            <html lang=""en"">
              <head>
                <meta charset=""UTF-8"">
                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
              </head>
              <body style=""font-family: Arial, sans-serif; margin: 0; padding: 0; background-color: #f8f9fa;"">
                <table role=""presentation"" style=""width: 100%; background-color: #f8f9fa;"" cellpadding=""0"" cellspacing=""0"">
                  <tr>
                    <td align=""center"">
                      <table role=""presentation"" class=""card"" style=""border: 1px solid #e0e0e0; border-radius: 4px; max-width: 600px; margin: 20px auto; background-color: white; box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);"" cellpadding=""0"" cellspacing=""0"">
                        <tr>
                          <td style=""background-color: #696cff; color: white; padding: 15px; font-size: 20px; text-align: center; border-radius: 4px 4px 0 0;"">
                            <strong>Notifikasi Booking Crane</strong>
                          </td>
                        </tr>
                        <tr>
                          <td style=""padding: 20px; color: #212529; font-size: 16px;"">
                            <p>Yth, Bapak/Ibu {name},</p>
                            <p>Pengajuan booking crane dengan nomor <strong>{booking.BookingNumber}</strong> untuk area <strong>{booking.Location}</strong> telah diajukan oleh <strong>#{booking.Department} / {createdBy}</strong>.</p>
                            <p>Detail Booking:</p>
                            <ul>
                              <li>Crane: <strong>{booking.Crane?.Code ?? "Unknown"}</strong></li>
                              <li>Tanggal Mulai: <strong>{booking.StartDate:dd/MM/yyyy}</strong></li>
                              <li>Tanggal Selesai: <strong>{booking.EndDate:dd/MM/yyyy}</strong></li>
                            </ul>
                            <p>Silakan cek link berikut untuk review dan approval:</p>
                            <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" style=""margin: 0 auto;"">
                              <tr>
                                <td align=""center"" style=""border-radius: 4px;"" bgcolor=""#696cff"">
                                  <a href=""{approvalUrl}"" target=""_blank"" style=""font-size: 16px; font-family: Arial, sans-serif; color: #ffffff; text-decoration: none; padding: 10px 20px; border-radius: 4px; background-color: #696cff; border: 1px solid #696cff; display: inline-block;"">
                                    Review & Approval
                                  </a>
                                </td>
                              </tr>
                            </table>
                            <div style=""color: #6c757d; font-size: 14px; margin-top: 20px;"">
                              <p>Terima Kasih,<br>
                              Crane Booking System<br>
                              <em>Terkirim otomatis oleh sistem.</em></p>
                            </div>
                          </td>
                        </tr>
                      </table>
                    </td>
                  </tr>
                </table>
              </body>
            </html>";
    }

    public string PicApprovalTemplate(string name, Booking booking, string createdBy, string badgeNumber, int stage)
    {
      byte[] bytesBN = Encoding.UTF8.GetBytes(badgeNumber);
      string encodedBN = Convert.ToBase64String(bytesBN);

      // Convert stage to string before encoding to Base64
      byte[] bytesStage = Encoding.UTF8.GetBytes(stage.ToString());
      string encodedStage = Convert.ToBase64String(bytesStage);

      string approvalUrl = $"{_baseUrl}/Approval/Pic?document_number={booking.Id}&badge_number={encodedBN}&stage={encodedStage}";

      return $@"<!doctype html>
            <html lang=""en"">
              <head>
                <meta charset=""UTF-8"">
                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
              </head>
              <body style=""font-family: Arial, sans-serif; margin: 0; padding: 0; background-color: #f8f9fa;"">
                <table role=""presentation"" style=""width: 100%; background-color: #f8f9fa;"" cellpadding=""0"" cellspacing=""0"">
                  <tr>
                    <td align=""center"">
                      <table role=""presentation"" class=""card"" style=""border: 1px solid #e0e0e0; border-radius: 4px; max-width: 600px; margin: 20px auto; background-color: white; box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);"" cellpadding=""0"" cellspacing=""0"">
                        <tr>
                          <td style=""background-color: #696cff; color: white; padding: 15px; font-size: 20px; text-align: center; border-radius: 4px 4px 0 0;"">
                            <strong>Notifikasi Booking Crane</strong>
                          </td>
                        </tr>
                        <tr>
                          <td style=""padding: 20px; color: #212529; font-size: 16px;"">
                            <p>Yth, Bapak/Ibu {name},</p>
                            <p>Pengajuan booking crane dengan nomor <strong>{booking.BookingNumber}</strong> untuk area <strong>{booking.Location}</strong> telah diajukan oleh <strong>#{booking.Department} / {createdBy}</strong> dan telah disetujui oleh Manager.</p>
                            <p>Detail Booking:</p>
                            <ul>
                              <li>Crane: <strong>{booking.Crane?.Code ?? "Unknown"}</strong></li>
                              <li>Tanggal Mulai: <strong>{booking.StartDate:dd/MM/yyyy}</strong></li>
                              <li>Tanggal Selesai: <strong>{booking.EndDate:dd/MM/yyyy}</strong></li>
                            </ul>
                            <p>Silakan cek link berikut untuk review dan approval:</p>
                            <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" style=""margin: 0 auto;"">
                              <tr>
                                <td align=""center"" style=""border-radius: 4px;"" bgcolor=""#696cff"">
                                  <a href=""{approvalUrl}"" target=""_blank"" style=""font-size: 16px; font-family: Arial, sans-serif; color: #ffffff; text-decoration: none; padding: 10px 20px; border-radius: 4px; background-color: #696cff; border: 1px solid #696cff; display: inline-block;"">
                                    Review & Approval
                                  </a>
                                </td>
                              </tr>
                            </table>
                            <div style=""color: #6c757d; font-size: 14px; margin-top: 20px;"">
                              <p>Terima Kasih,<br>
                              Crane Booking System<br>
                              <em>Terkirim otomatis oleh sistem.</em></p>
                            </div>
                          </td>
                        </tr>
                      </table>
                    </td>
                  </tr>
                </table>
              </body>
            </html>";
    }

    public string BookingManagerApprovedTemplate(string name, Booking booking)
    {
      string detailUrl = $"{_baseUrl}/BookingHistory/Details/{booking.Id}";

      return $@"<!doctype html>
            <html lang=""en"">
              <head>
                <meta charset=""UTF-8"">
                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
              </head>
              <body style=""font-family: Arial, sans-serif; margin: 0; padding: 0; background-color: #f8f9fa;"">
                <table role=""presentation"" style=""width: 100%; background-color: #f8f9fa;"" cellpadding=""0"" cellspacing=""0"">
                  <tr>
                    <td align=""center"">
                      <table role=""presentation"" class=""card"" style=""border: 1px solid #e0e0e0; border-radius: 4px; max-width: 600px; margin: 20px auto; background-color: white; box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);"" cellpadding=""0"" cellspacing=""0"">
                        <tr>
                          <td style=""background-color: #71dd37; color: white; padding: 15px; font-size: 20px; text-align: center; border-radius: 4px 4px 0 0;"">
                            <strong>Notifikasi Booking Crane</strong>
                          </td>
                        </tr>
                        <tr>
                          <td style=""padding: 20px; color: #212529; font-size: 16px;"">
                            <p>Yth, Bapak/Ibu {name},</p>
                            <p>Pengajuan booking crane dengan nomor <strong>{booking.BookingNumber}</strong> telah disetujui oleh Manager {booking.ManagerName}.</p>
                            <p>Booking Anda saat ini sedang menunggu persetujuan dari PIC Crane. Kami akan memberitahu Anda saat ada pembaruan.</p>
                            <p>Silakan cek link berikut untuk melihat status booking:</p>
                            <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" style=""margin: 0 auto;"">
                              <tr>
                                <td align=""center"" style=""border-radius: 4px;"" bgcolor=""#71dd37"">
                                  <a href=""{detailUrl}"" target=""_blank"" style=""font-size: 16px; font-family: Arial, sans-serif; color: #ffffff; text-decoration: none; padding: 10px 20px; border-radius: 4px; background-color: #71dd37; border: 1px solid #71dd37; display: inline-block;"">
                                    Lihat Detail Booking
                                  </a>
                                </td>
                              </tr>
                            </table>
                            <div style=""color: #6c757d; font-size: 14px; margin-top: 20px;"">
                              <p>Terima Kasih,<br>
                              Crane Booking System<br>
                              <em>Terkirim otomatis oleh sistem.</em></p>
                            </div>
                          </td>
                        </tr>
                      </table>
                    </td>
                  </tr>
                </table>
              </body>
            </html>";
    }

    public string BookingApprovedTemplate(string name, Booking booking)
    {
      string detailUrl = $"{_baseUrl}/BookingHistory/Details/{booking.Id}";

      return $@"<!doctype html>
            <html lang=""en"">
              <head>
                <meta charset=""UTF-8"">
                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
              </head>
              <body style=""font-family: Arial, sans-serif; margin: 0; padding: 0; background-color: #f8f9fa;"">
                <table role=""presentation"" style=""width: 100%; background-color: #f8f9fa;"" cellpadding=""0"" cellspacing=""0"">
                  <tr>
                    <td align=""center"">
                      <table role=""presentation"" class=""card"" style=""border: 1px solid #e0e0e0; border-radius: 4px; max-width: 600px; margin: 20px auto; background-color: white; box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);"" cellpadding=""0"" cellspacing=""0"">
                        <tr>
                          <td style=""background-color: #71dd37; color: white; padding: 15px; font-size: 20px; text-align: center; border-radius: 4px 4px 0 0;"">
                            <strong>Notifikasi Booking Crane | Selesai</strong>
                          </td>
                        </tr>
                        <tr>
                          <td style=""padding: 20px; color: #212529; font-size: 16px;"">
                            <p>Yth, Bapak/Ibu {name},</p>
                            <p>Pengajuan booking crane dengan nomor <strong>{booking.BookingNumber}</strong> telah disetujui sepenuhnya.</p>
                            <p>Detail Booking:</p>
                            <ul>
                              <li>Crane: <strong>{booking.Crane?.Code ?? "Unknown"}</strong></li>
                              <li>Lokasi: <strong>{booking.Location}</strong></li>
                              <li>Tanggal Mulai: <strong>{booking.StartDate:dd/MM/yyyy}</strong></li>
                              <li>Tanggal Selesai: <strong>{booking.EndDate:dd/MM/yyyy}</strong></li>
                            </ul>
                            <p>Seluruh approval yang diperlukan telah didapatkan, silahkan cek link di bawah untuk detilnya.</p>
                            <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" style=""margin: 0 auto;"">
                              <tr>
                                <td align=""center"" style=""border-radius: 4px;"" bgcolor=""#71dd37"">
                                  <a href=""{detailUrl}"" target=""_blank"" style=""font-size: 16px; font-family: Arial, sans-serif; color: #ffffff; text-decoration: none; padding: 10px 20px; border-radius: 4px; background-color: #71dd37; border: 1px solid #71dd37; display: inline-block;"">
                                    Lihat Detail Booking
                                  </a>
                                </td>
                              </tr>
                            </table>
                            <div style=""color: #6c757d; font-size: 14px; margin-top: 20px;"">
                              <p>Terima Kasih,<br>
                              Crane Booking System<br>
                              <em>Terkirim otomatis oleh sistem.</em></p>
                            </div>
                          </td>
                        </tr>
                      </table>
                    </td>
                  </tr>
                </table>
              </body>
            </html>";
    }

    public string BookingRejectedTemplate(string name, Booking booking, string rejectorName, string rejectReason)
    {
      string detailUrl = $"{_baseUrl}/BookingHistory/Details/{booking.Id}";

      return $@"<!doctype html>
            <html lang=""en"">
              <head>
                <meta charset=""UTF-8"">
                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
              </head>
              <body style=""font-family: Arial, sans-serif; margin: 0; padding: 0; background-color: #f8f9fa;"">
                <table role=""presentation"" style=""width: 100%; background-color: #f8f9fa;"" cellpadding=""0"" cellspacing=""0"">
                  <tr>
                    <td align=""center"">
                      <table role=""presentation"" class=""card"" style=""border: 1px solid #e0e0e0; border-radius: 4px; max-width: 600px; margin: 20px auto; background-color: white; box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);"" cellpadding=""0"" cellspacing=""0"">
                        <tr>
                          <td style=""background-color: #ffab00; color: white; padding: 15px; font-size: 20px; text-align: center; border-radius: 4px 4px 0 0;"">
                            <strong>Notifikasi Booking Crane | Ditolak</strong>
                          </td>
                        </tr>
                        <tr>
                          <td style=""padding: 20px; color: #212529; font-size: 16px;"">
                            <p>Yth, Bapak/Ibu {name},</p>
                            <p>Pengajuan booking crane dengan nomor <strong>{booking.BookingNumber}</strong> telah ditolak oleh {rejectorName}.</p>
                            <p>Detail Booking:</p>
                            <ul>
                              <li>Crane: <strong>{booking.Crane?.Code ?? "Unknown"}</strong></li>
                              <li>Lokasi: <strong>{booking.Location}</strong></li>
                              <li>Tanggal Mulai: <strong>{booking.StartDate:dd/MM/yyyy}</strong></li>
                              <li>Tanggal Selesai: <strong>{booking.EndDate:dd/MM/yyyy}</strong></li>
                            </ul>
                            <p>Alasan Penolakan: {rejectReason}</p>
                            <p>Silahkan untuk memperbaharui data yang diminta:</p>
                            <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" style=""margin: 0 auto;"">
                              <tr>
                                <td align=""center"" style=""border-radius: 4px;"" bgcolor=""#ffab00"">
                                  <a href=""{detailUrl}"" target=""_blank"" style=""font-size: 16px; font-family: Arial, sans-serif; color: #ffffff; text-decoration: none; padding: 10px 20px; border-radius: 4px; background-color: #ffab00; border: 1px solid #ffab00; display: inline-block;"">
                                    Lihat Detail Booking
                                  </a>
                                </td>
                              </tr>
                            </table>
                            <div style=""color: #6c757d; font-size: 14px; margin-top: 20px;"">
                              <p>Terima Kasih,<br>
                              Crane Booking System<br>
                              <em>Terkirim otomatis oleh sistem.</em></p>
                            </div>
                          </td>
                        </tr>
                      </table>
                    </td>
                  </tr>
                </table>
              </body>
            </html>";
    }

    // Services/Email/EmailTemplate.cs
    // Add these methods to the existing EmailTemplate class

    public string BookingCancelledTemplate(string name, Booking booking, string cancelledBy, string cancelReason)
    {
      string detailUrl = $"{_baseUrl}/BookingHistory/Details/{booking.Id}";

      return $@"<!doctype html>
        <html lang=""en"">
          <head>
            <meta charset=""UTF-8"">
            <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
          </head>
          <body style=""font-family: Arial, sans-serif; margin: 0; padding: 0; background-color: #f8f9fa;"">
            <table role=""presentation"" style=""width: 100%; background-color: #f8f9fa;"" cellpadding=""0"" cellspacing=""0"">
              <tr>
                <td align=""center"">
                  <table role=""presentation"" class=""card"" style=""border: 1px solid #e0e0e0; border-radius: 4px; max-width: 600px; margin: 20px auto; background-color: white; box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);"" cellpadding=""0"" cellspacing=""0"">
                    <tr>
                      <td style=""background-color: #ff3e1d; color: white; padding: 15px; font-size: 20px; text-align: center; border-radius: 4px 4px 0 0;"">
                        <strong>Notifikasi Booking Crane | Dibatalkan</strong>
                      </td>
                    </tr>
                    <tr>
                      <td style=""padding: 20px; color: #212529; font-size: 16px;"">
                        <p>Yth, Bapak/Ibu {name},</p>
                        <p>Booking crane dengan nomor <strong>{booking.BookingNumber}</strong> telah dibatalkan oleh {cancelledBy}.</p>
                        <p>Detail Booking:</p>
                        <ul>
                          <li>Crane: <strong>{booking.Crane?.Code ?? "Unknown"}</strong></li>
                          <li>Lokasi: <strong>{booking.Location}</strong></li>
                          <li>Tanggal Mulai: <strong>{booking.StartDate:dd/MM/yyyy}</strong></li>
                          <li>Tanggal Selesai: <strong>{booking.EndDate:dd/MM/yyyy}</strong></li>
                        </ul>
                        <p>Alasan Pembatalan: {cancelReason}</p>
                        <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" style=""margin: 0 auto;"">
                          <tr>
                            <td align=""center"" style=""border-radius: 4px;"" bgcolor=""#ff3e1d"">
                              <a href=""{detailUrl}"" target=""_blank"" style=""font-size: 16px; font-family: Arial, sans-serif; color: #ffffff; text-decoration: none; padding: 10px 20px; border-radius: 4px; background-color: #ff3e1d; border: 1px solid #ff3e1d; display: inline-block;"">
                                Lihat Detail Booking
                              </a>
                            </td>
                          </tr>
                        </table>
                        <div style=""color: #6c757d; font-size: 14px; margin-top: 20px;"">
                          <p>Terima Kasih,<br>
                          Crane Booking System<br>
                          <em>Terkirim otomatis oleh sistem.</em></p>
                        </div>
                      </td>
                    </tr>
                  </table>
                </td>
              </tr>
            </table>
          </body>
        </html>";
    }

    public string BookingRevisedTemplate(string name, Booking booking)
    {
      string detailUrl = $"{_baseUrl}/BookingHistory/Details/{booking.Id}";

      return $@"<!doctype html>
        <html lang=""en"">
          <head>
            <meta charset=""UTF-8"">
            <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
          </head>
          <body style=""font-family: Arial, sans-serif; margin: 0; padding: 0; background-color: #f8f9fa;"">
            <table role=""presentation"" style=""width: 100%; background-color: #f8f9fa;"" cellpadding=""0"" cellspacing=""0"">
              <tr>
                <td align=""center"">
                  <table role=""presentation"" class=""card"" style=""border: 1px solid #e0e0e0; border-radius: 4px; max-width: 600px; margin: 20px auto; background-color: white; box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);"" cellpadding=""0"" cellspacing=""0"">
                    <tr>
                      <td style=""background-color: #03c3ec; color: white; padding: 15px; font-size: 20px; text-align: center; border-radius: 4px 4px 0 0;"">
                        <strong>Notifikasi Booking Crane | Revisi Diajukan</strong>
                      </td>
                    </tr>
                    <tr>
                      <td style=""padding: 20px; color: #212529; font-size: 16px;"">
                        <p>Yth, Bapak/Ibu {name},</p>
                        <p>Booking crane dengan nomor <strong>{booking.BookingNumber}</strong> telah direvisi dan diajukan kembali.</p>
                        <p>Detail Booking:</p>
                        <ul>
                          <li>Crane: <strong>{booking.Crane?.Code ?? "Unknown"}</strong></li>
                          <li>Lokasi: <strong>{booking.Location}</strong></li>
                          <li>Tanggal Mulai: <strong>{booking.StartDate:dd/MM/yyyy}</strong></li>
                          <li>Tanggal Selesai: <strong>{booking.EndDate:dd/MM/yyyy}</strong></li>
                          <li>Jumlah Revisi: <strong>{booking.RevisionCount}</strong></li>
                          <li>Terakhir Diubah Oleh: <strong>{booking.LastModifiedBy}</strong></li>
                          <li>Terakhir Diubah Pada: <strong>{booking.LastModifiedAt:dd/MM/yyyy HH:mm}</strong></li>
                        </ul>
                        <p>Booking ini memerlukan persetujuan kembali. Silakan cek link berikut untuk detail:</p>
                        <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" style=""margin: 0 auto;"">
                          <tr>
                            <td align=""center"" style=""border-radius: 4px;"" bgcolor=""#03c3ec"">
                              <a href=""{detailUrl}"" target=""_blank"" style=""font-size: 16px; font-family: Arial, sans-serif; color: #ffffff; text-decoration: none; padding: 10px 20px; border-radius: 4px; background-color: #03c3ec; border: 1px solid #03c3ec; display: inline-block;"">
                                Lihat Detail Booking
                              </a>
                            </td>
                          </tr>
                        </table>
                        <div style=""color: #6c757d; font-size: 14px; margin-top: 20px;"">
                          <p>Terima Kasih,<br>
                          Crane Booking System<br>
                          <em>Terkirim otomatis oleh sistem.</em></p>
                        </div>
                      </td>
                    </tr>
                  </table>
                </td>
              </tr>
            </table>
          </body>
        </html>";
    }
  }
}
