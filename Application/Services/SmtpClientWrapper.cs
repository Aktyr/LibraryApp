using ISmtpClient = LibApp.Application.Interfaces.ISmtpClient;
using MailKitAuthException = MailKit.Security.AuthenticationException;
using MailKitCommandException = MailKit.Net.Smtp.SmtpCommandException;
using MailKitNotAuthenticatedException = MailKit.ServiceNotAuthenticatedException;
using MailKitNotConnectedException = MailKit.ServiceNotConnectedException;
using MailKitProtocolException = MailKit.Net.Smtp.SmtpProtocolException;
using MailKitSmtpClient = MailKit.Net.Smtp.SmtpClient;
using NetMailMessage = System.Net.Mail.MailMessage;
using NetSmtpException = System.Net.Mail.SmtpException;
using NetSmtpStatusCode = System.Net.Mail.SmtpStatusCode;

namespace LibApp.Application.Services;

/// <summary>
/// SMTP-обёртка на MailKit. Принимает <see cref="System.Net.Mail.MailMessage"/>,
/// чтобы не менять контракт <c>ISmtpClient</c> и не затрагивать <c>EmailNotificationService</c>.
/// Исключения MailKit адаптируются в <see cref="System.Net.Mail.SmtpException"/>, чтобы
/// вызывающий код и тесты продолжали работать с привычным типом.
/// </summary>
public sealed class SmtpClientWrapper : ISmtpClient
{
    // Таймаут SMTP-операций. Дефолт MailKit — 120 секунд: при недоступном SMTP
    // ConnectAsync висит всё это время, HTTP-запрос из UI не возвращается,
    // модалка «Одобрить заявку» крутится. 15 секунд — баланс между «успеть
    // при медленном, но живом SMTP» и «не блокировать UI на минуты».
    private const int SmtpTimeoutMs = 15_000;

    private readonly NotificationSettings _settings;

    public SmtpClientWrapper(IOptions<NotificationSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SendMailAsync(NetMailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var mimeMessage = MimeMessage.CreateFromMailMessage(message);

        using var client = new MailKitSmtpClient
        {
            Timeout = SmtpTimeoutMs
        };

        try
        {
            // 465 — TLS с самого начала (SMTPS), флаг RequireTls не влияет.
            // 587/25/2525 — StartTls, если провайдер его поддерживает.
            // RequireTls = false — для smtp4dev без TLS; в production всегда true.
            var socketOptions = _settings.SmtpPort switch
            {
                465 => SecureSocketOptions.SslOnConnect,
                _ when _settings.RequireTls => SecureSocketOptions.StartTls,
                _ => SecureSocketOptions.None
            };

            await client.ConnectAsync(
                _settings.SmtpServer,
                _settings.SmtpPort,
                socketOptions,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(_settings.SmtpUsername))
            {
                await client.AuthenticateAsync(
                    _settings.SmtpUsername,
                    _settings.SmtpPassword,
                    cancellationToken);
            }

            await client.SendAsync(mimeMessage, cancellationToken);
            await client.DisconnectAsync(quit: true, cancellationToken);
        }
        catch (MailKitAuthException ex)
        {
            throw new NetSmtpException(ex.Message, ex);
        }
        catch (MailKitCommandException ex)
        {
            // StatusCode — enum MailKit.Net.Smtp.SmtpStatusCode. Приводим к int,
            // чтобы case-метки оставались числами (C# не разрешает int-литералы в switch по enum).
            var code = (int)ex.StatusCode;
            var status = code switch
            {
                535 => NetSmtpStatusCode.ClientNotPermitted,
                550 or 551 or 552 or 553 => NetSmtpStatusCode.MailboxUnavailable,
                421 or 450 or 451 or 452 => NetSmtpStatusCode.ServiceNotAvailable,
                _ => NetSmtpStatusCode.GeneralFailure
            };
            throw new NetSmtpException(status, ex.Message);
        }
        catch (MailKitProtocolException ex)
        {
            throw new NetSmtpException(ex.Message, ex);
        }
        catch (MailKitNotConnectedException ex)
        {
            throw new NetSmtpException(ex.Message, ex);
        }
        catch (MailKitNotAuthenticatedException ex)
        {
            throw new NetSmtpException(ex.Message, ex);
        }
        catch (OperationCanceledException)
        {
            // Сработал Timeout или внешний CancellationToken.
            // Оборачиваем в SmtpException, чтобы вызывающий код и тесты не различали
            // «отмена клиентом» и «сервер не ответил» — для UX это один сценарий.
            throw new NetSmtpException("SMTP operation timed out or was cancelled");
        }
    }

    public void Dispose()
    {
        // MailKit SmtpClient создаётся и уничтожается внутри SendMailAsync.
        // Долгоживущего ресурса нет.
    }
}