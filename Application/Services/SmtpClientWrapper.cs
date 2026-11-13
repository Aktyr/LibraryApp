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
    private readonly NotificationSettings _settings;

    public SmtpClientWrapper(IOptions<NotificationSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SendMailAsync(NetMailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var mimeMessage = MimeMessage.CreateFromMailMessage(message);

        using var client = new MailKitSmtpClient();

        try
        {
            // StartTls на 587 — стандарт для Brevo. SslOnConnect на 465 — альтернативный порт.
            var socketOptions = _settings.SmtpPort == 465
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;

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
            // SmtpException не имеет 3-аргументного конструктора (statusCode, message, inner).
            // Сохраняем message и inner: они нужны для логов и диагностики SMTP-проблем.
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
    }

    public void Dispose()
    {
        // MailKit SmtpClient создаётся и уничтожается внутри SendMailAsync.
        // Долгоживущего ресурса нет.
    }
}