namespace ArtemisBank.Core.Application.Common.Constants
{
    public static class BrandedEmail
    {
        private const string Night = "#0d1b33";
        private const string Deep = "#14274a";
        private const string Brass = "#b3862f";
        private const string Paper = "#f7f8fb";
        private const string Border = "#dfe4ec";
        private const string Slate = "#41546f";
        private const string Body = "#1d2739";

        private const string SansFamily =
            "-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif";
        private const string SerifFamily =
            "Georgia, 'Times New Roman', Times, serif";

        public static string Build(
            string preheader,
            string heading,
            string greeting,
            string intro,
            string buttonText,
            string buttonUrl,
            string expirationNote,
            string closingNote)
        {
            return $"""
<!DOCTYPE html>
<html lang="es">
<head>
<meta charset="utf-8" />
<meta name="viewport" content="width=device-width, initial-scale=1" />
<title>{heading}</title>
</head>
<body style="margin:0;padding:0;background-color:{Paper};">

<div style="display:none;font-size:1px;color:{Paper};max-height:0;overflow:hidden;">
{preheader}
</div>

<table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0"
       style="background-color:{Paper};padding:32px 12px;">
<tr>
<td align="center">

<table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0"
       style="max-width:560px;background-color:#ffffff;border:1px solid {Border};
              border-radius:10px;overflow:hidden;">

<tr>
<td style="background-color:{Night};padding:28px 32px;border-bottom:3px solid {Brass};">
<table role="presentation" cellpadding="0" cellspacing="0" border="0">
<tr>
<td style="padding-right:10px;font-size:26px;line-height:1;color:{Brass};">&#127963;</td>
<td style="font-family:{SerifFamily};font-size:21px;font-weight:bold;color:#ffffff;
           letter-spacing:-0.02em;line-height:1.1;">
Artemis Banking Pro
</td>
</tr>
</table>
</td>
</tr>

<tr>
<td style="padding:32px;">

<h1 style="margin:0 0 20px;font-family:{SerifFamily};font-size:21px;font-weight:normal;
           color:{Deep};line-height:1.3;">
{heading}
</h1>

<p style="margin:0 0 14px;font-family:{SansFamily};font-size:15px;color:{Body};line-height:1.6;">
{greeting}
</p>

<p style="margin:0 0 26px;font-family:{SansFamily};font-size:15px;color:{Body};line-height:1.6;">
{intro}
</p>

<table role="presentation" cellpadding="0" cellspacing="0" border="0" style="margin:0 0 26px;">
<tr>
<td style="background-color:{Deep};border-radius:6px;">
<a href="{buttonUrl}"
   style="display:inline-block;padding:13px 30px;font-family:{SansFamily};font-size:15px;
          font-weight:bold;color:#ffffff;text-decoration:none;border-radius:6px;">
{buttonText}
</a>
</td>
</tr>
</table>

<p style="margin:0 0 8px;font-family:{SansFamily};font-size:13px;color:{Slate};line-height:1.6;">
Si el botón no funciona, copie y pegue esta dirección en su navegador:
</p>

<p style="margin:0 0 26px;padding:12px 14px;background-color:{Paper};border:1px solid {Border};
          border-radius:6px;font-family:'Courier New', Courier, monospace;font-size:12px;
          color:{Deep};line-height:1.5;word-break:break-all;">
{buttonUrl}
</p>

<p style="margin:0 0 6px;font-family:{SansFamily};font-size:13px;color:{Slate};line-height:1.6;">
{expirationNote}
</p>

<p style="margin:0;font-family:{SansFamily};font-size:13px;color:{Slate};line-height:1.6;">
{closingNote}
</p>

</td>
</tr>

<tr>
<td style="background-color:{Paper};padding:20px 32px;border-top:1px solid {Border};">
<p style="margin:0 0 4px;font-family:{SansFamily};font-size:12px;color:{Slate};line-height:1.5;">
Este es un mensaje automático. Por favor no responda a este correo.
</p>
<p style="margin:0;font-family:{SansFamily};font-size:12px;color:{Slate};line-height:1.5;">
Artemis Banking Pro nunca le pedirá su contraseña por correo ni por teléfono.
</p>
</td>
</tr>

</table>

</td>
</tr>
</table>

</body>
</html>
""";
        }

        public static string Activation(string firstName, string link) => Build(
            preheader: "Active su cuenta de Artemis Banking Pro.",
            heading: "Active su cuenta",
            greeting: $"Hola {firstName},",
            intro: "Su cuenta ha sido creada. Para empezar a utilizarla, confirme su correo "
                 + "electrónico con el siguiente botón.",
            buttonText: "Activar mi cuenta",
            buttonUrl: link,
            expirationNote: "Este enlace es de un solo uso.",
            closingNote: "Si usted no solicitó esta cuenta, ignore este mensaje.");

        public static string PasswordReset(string firstName, string link) => Build(
            preheader: "Restablezca la contraseña de su cuenta.",
            heading: "Restablecer contraseña",
            greeting: $"Hola {firstName},",
            intro: "Hemos recibido una solicitud para restablecer la contraseña de su cuenta. "
                 + "Pulse el botón para elegir una nueva.",
            buttonText: "Restablecer mi contraseña",
            buttonUrl: link,
            expirationNote: "Este enlace tendrá una vigencia de 30 minutos y es de un solo uso.",
            closingNote: "Si usted no solicitó este cambio, ignore este mensaje. "
                       + "Su contraseña actual seguirá siendo válida.");
    }
}
