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
      // string detailUrl = $"{_baseUrl}/BookingHistory/Details/{booking.Id}";
      string detailUrl = $"{_baseUrl}/Booking/Details?documentNumber={booking.DocumentNumber}";

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

      string approvalUrl = $"{_baseUrl}/Approval/Manager?document_number={booking.DocumentNumber}&badge_number={encodedBN}&stage={encodedStage}";

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

      string approvalUrl = $"{_baseUrl}/Approval/Pic?document_number={booking.DocumentNumber}&badge_number={encodedBN}&stage={encodedStage}";

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
      // string detailUrl = $"{_baseUrl}/BookingHistory/Details/{booking.Id}";
      string detailUrl = $"{_baseUrl}/Booking/Details?documentNumber={booking.DocumentNumber}";

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
      // string detailUrl = $"{_baseUrl}/BookingHistory/Details/{booking.Id}";
      string detailUrl = $"{_baseUrl}/Booking/Details?documentNumber={booking.DocumentNumber}";

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
      // string detailUrl = $"{_baseUrl}/BookingHistory/Details/{booking.Id}";
      string detailUrl = $"{_baseUrl}/Booking/Details?documentNumber={booking.DocumentNumber}";

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
      // string detailUrl = $"{_baseUrl}/BookingHistory/Details/{booking.Id}";
      string detailUrl = $"{_baseUrl}/Booking/Details?documentNumber={booking.DocumentNumber}";

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
      // string detailUrl = $"{_baseUrl}/BookingHistory/Details/{booking.Id}";
      string detailUrl = $"{_baseUrl}/Booking/Details?documentNumber={booking.DocumentNumber}";

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

    // Services/Email/EmailTemplate.cs - Add this method
    public string BookingAffectedByBreakdownTemplate(string userName, Booking booking, Breakdown breakdown)
    {
      string detailUrl = $"{_baseUrl}/Booking/Details?documentNumber={booking.DocumentNumber}";

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
                  <td style=""background-color: #ff6b6b; color: white; padding: 15px; font-size: 20px; text-align: center; border-radius: 4px 4px 0 0;"">
                    <strong>⚠️ Booking Terdampak Breakdown Crane</strong>
                  </td>
                </tr>
                <tr>
                  <td style=""padding: 20px; color: #212529; font-size: 16px;"">
                    <p>Yth, Bapak/Ibu <strong>{userName}</strong>,</p>

                    <p>Kami informasikan bahwa crane yang Anda booking mengalami breakdown/maintenance dan dapat mempengaruhi jadwal booking Anda.</p>

                    <div style=""background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 4px; margin: 15px 0;"">
                      <h4 style=""color: #856404; margin-top: 0;"">📋 Detail Booking Anda:</h4>
                      <ul style=""color: #856404; margin-bottom: 0;"">
                        <li><strong>Nomor Booking:</strong> {booking.BookingNumber}</li>
                        <li><strong>Crane:</strong> {booking.CraneCode ?? "Unknown"}</li>
                        <li><strong>Lokasi:</strong> {booking.Location}</li>
                        <li><strong>Periode Booking:</strong> {booking.StartDate:dd/MM/yyyy} - {booking.EndDate:dd/MM/yyyy}</li>
                      </ul>
                    </div>

                    <div style=""background-color: #f8d7da; border: 1px solid #f5c6cb; padding: 15px; border-radius: 4px; margin: 15px 0;"">
                      <h4 style=""color: #721c24; margin-top: 0;"">🔧 Detail Breakdown:</h4>
                      <ul style=""color: #721c24; margin-bottom: 0;"">
                        <li><strong>Waktu Mulai:</strong> {breakdown.UrgentStartTime:dd/MM/yyyy HH:mm}</li>
                        <li><strong>Estimasi Selesai:</strong> {breakdown.UrgentEndTime:dd/MM/yyyy HH:mm}</li>
                        <li><strong>Alasan:</strong> {breakdown.Reasons}</li>
                      </ul>
                    </div>

                    <p><strong>Tindakan yang perlu Anda lakukan:</strong></p>
                    <ol>
                      <li>Hubungi PIC Crane untuk koordinasi lebih lanjut</li>
                      <li>Pertimbangkan untuk mengajukan perubahan jadwal jika diperlukan</li>
                      <li>Monitor status crane melalui sistem</li>
                    </ol>

                    <p>Silakan klik tombol di bawah untuk melihat detail booking Anda:</p>

                    <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" style=""margin: 20px auto;"">
                      <tr>
                        <td align=""center"" style=""border-radius: 4px;"" bgcolor=""#ff6b6b"">
                          <a href=""{detailUrl}"" target=""_blank"" style=""font-size: 16px; font-family: Arial, sans-serif; color: #ffffff; text-decoration: none; padding: 12px 25px; border-radius: 4px; background-color: #ff6b6b; border: 1px solid #ff6b6b; display: inline-block;"">
                            📋 Lihat Detail Booking
                          </a>
                        </td>
                      </tr>
                    </table>

                    <div style=""background-color: #e2e3e5; padding: 15px; border-radius: 4px; margin-top: 20px;"">
                      <p style=""margin: 0; font-size: 14px; color: #6c757d;"">
                        <strong>💡 Tips:</strong> Booking Anda tetap aktif. Jika breakdown selesai lebih cepat dari estimasi, crane akan otomatis tersedia kembali.
                      </p>
                    </div>

                    <div style=""color: #6c757d; font-size: 14px; margin-top: 25px; border-top: 1px solid #dee2e6; padding-top: 15px;"">
                      <p>Mohon maaf atas ketidaknyamanan ini.<br>
                      <strong>Tim Crane Booking System</strong><br>
                      <em>Terkirim otomatis oleh sistem pada {DateTime.Now:dd/MM/yyyy HH:mm}</em></p>
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

    // Services/Email/EmailTemplate.cs - Add this method
    public string BookingReminderTemplate(string userName, Booking booking)
    {
      string detailUrl = $"{_baseUrl}/Booking/Details?documentNumber={booking.DocumentNumber}";

      // Format shifts info
      var shiftsInfo = "";
      if (booking.BookingShifts?.Any() == true)
      {
        var shiftsForFirstDay = booking.BookingShifts
            .Where(s => s.Date.Date == booking.StartDate.Date)
            .OrderBy(s => s.ShiftStartTime)
            .ToList();

        shiftsInfo = string.Join(", ", shiftsForFirstDay.Select(s =>
        {
          var startTime = DateTime.Today.Add(s.ShiftStartTime).ToString("HH:mm");
          var endTime = DateTime.Today.Add(s.ShiftEndTime).ToString("HH:mm");
          return $"{s.ShiftName} ({startTime}-{endTime})";
        }));
      }

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
                  <td style=""background-color: #28a745; color: white; padding: 15px; font-size: 20px; text-align: center; border-radius: 4px 4px 0 0;"">
                    <strong>🔔 Pengingat Booking Crane Besok</strong>
                  </td>
                </tr>
                <tr>
                  <td style=""padding: 20px; color: #212529; font-size: 16px;"">
                    <p>Yth, Bapak/Ibu <strong>{userName}</strong>,</p>

                    <p>Kami ingatkan bahwa Anda memiliki booking crane yang akan dimulai <strong>besok</strong>. Mohon persiapkan segala kebutuhan operasional Anda.</p>

                    <div style=""background-color: #d4edda; border: 1px solid #c3e6cb; padding: 15px; border-radius: 4px; margin: 15px 0;"">
                      <h4 style=""color: #155724; margin-top: 0;"">📋 Detail Booking Besok:</h4>
                      <ul style=""color: #155724; margin-bottom: 0;"">
                        <li><strong>Nomor Booking:</strong> {booking.BookingNumber}</li>
                        <li><strong>Crane:</strong> {booking.CraneCode ?? "Unknown"}</li>
                        <li><strong>Tanggal:</strong> {booking.StartDate:dddd, dd MMMM yyyy}</li>
                        <li><strong>Shift:</strong> {shiftsInfo}</li>
                        <li><strong>Lokasi:</strong> {booking.Location}</li>
                        <li><strong>Project Supervisor:</strong> {booking.ProjectSupervisor ?? "-"}</li>
                        <li><strong>Cost Code:</strong> {booking.CostCode ?? "-"}</li>
                      </ul>
                    </div>

                    <div style=""background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 4px; margin: 15px 0;"">
                      <h4 style=""color: #856404; margin-top: 0;"">📝 Checklist Persiapan:</h4>
                      <ul style=""color: #856404; margin-bottom: 0;"">
                        <li>✅ Pastikan tim operator dan pengawal sudah siap</li>
                        <li>✅ Koordinasi dengan PIC Crane untuk briefing keselamatan</li>
                        <li>✅ Persiapkan peralatan dan material yang akan diangkat</li>
                        <li>✅ Pastikan area kerja sudah aman dan bebas hambatan</li>
                        <li>✅ Cek kondisi cuaca dan faktor keselamatan lainnya</li>
                      </ul>
                    </div>

                    <p>Jika ada perubahan mendadak atau pembatalan, segera koordinasikan dengan PIC Crane.</p>

                    <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"" style=""margin: 20px auto;"">
                      <tr>
                        <td align=""center"" style=""border-radius: 4px;"" bgcolor=""#28a745"">
                          <a href=""{detailUrl}"" target=""_blank"" style=""font-size: 16px; font-family: Arial, sans-serif; color: #ffffff; text-decoration: none; padding: 12px 25px; border-radius: 4px; background-color: #28a745; border: 1px solid #28a745; display: inline-block;"">
                            📋 Lihat Detail Booking
                          </a>
                        </td>
                      </tr>
                    </table>

                    <div style=""background-color: #e2e3e5; padding: 15px; border-radius: 4px; margin-top: 20px;"">
                      <p style=""margin: 0; font-size: 14px; color: #6c757d;"">
                        <strong>💡 Tips:</strong> Datang 15 menit lebih awal untuk koordinasi dan safety briefing dengan operator crane.
                      </p>
                    </div>

                    <div style=""color: #6c757d; font-size: 14px; margin-top: 25px; border-top: 1px solid #dee2e6; padding-top: 15px;"">
                      <p>Selamat bekerja dan utamakan keselamatan!<br>
                      <strong>Tim Crane Booking System</strong><br>
                      <em>Pengingat otomatis - dikirim {DateTime.Now:dd/MM/yyyy HH:mm}</em></p>
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
