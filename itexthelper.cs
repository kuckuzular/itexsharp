
        private static byte[] AddImage(AsymmetricCipherKeyPair kp, Stream inputPdfStream, string consultantName)
        {
            var newCert = Functions.GenerateCertificate(kp, consultantName);
            var pk = kp.Public;

            using (MemoryStream outputPdfStream = new MemoryStream())
            {
                //To get the location the assembly normally resides on disk or the install directory
                string path = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
                var directory = System.IO.Path.GetDirectoryName(path);

                Image image = Image.GetInstance(directory + "/logo.png");
                image.ScaleToFit(100, 100);

                var reader = new PdfReader(inputPdfStream);
                PdfStamper stamper = PdfStamper.CreateSignature(reader, outputPdfStream, '\0');
                {
                    MyLocationTextExtractionStrategy textPos = new MyLocationTextExtractionStrategy("digital", System.Globalization.CompareOptions.IgnoreCase);

                    if (textPos != null)
                    {
                        iTextSharp.text.pdf.parser.PdfTextExtractor.GetTextFromPage(reader, 1, textPos);
                        PdfContentByte content = stamper.GetOverContent(1);
                        var point = textPos.myPoints.FirstOrDefault();

                        if (point != null)
                        {
                            Phrase dataText = new Phrase(consultantName);

                            BaseFont baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.WINANSI, BaseFont.EMBEDDED);

                            image.SetAbsolutePosition(point.Rect.Left, point.Rect.Top);
                            image.SetDpi(380, 117);

                            //PdfContentByte waterMark;
                            PdfGState graphicsState = new PdfGState();
                            graphicsState.FillOpacity = 0.4F;  // (or whatever)
                            graphicsState.StrokeOpacity = 0.5f;

                            content.SetGState(graphicsState);

                            content.AddImage(image);
                            graphicsState = new PdfGState();
                            graphicsState.FillOpacity = 1F;  // (or whatever)
                            graphicsState.StrokeOpacity = 1f;

                            content.BeginText();
                            BaseFont f_cn = BaseFont.CreateFont(@"C:\Windows\Fonts\LHANDW.TTF", BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                            content.SetFontAndSize(f_cn, 14);
                            content.SetTextMatrix(point.Rect.Left, point.Rect.Top - 7);  //(xPos, yPos)
                            content.ShowText(consultantName);
                            content.EndText();
                            content.SetGState(graphicsState);
                            content.BeginText();
                            content.SetFontAndSize(baseFont, 8);
                            content.SetTextMatrix(point.Rect.Left, point.Rect.Top - 15);  //(xPos, yPos)
                            content.ShowText(string.Concat("Digitally signed by ", consultantName));
                            content.EndText();
                            content.BeginText();
                            content.SetTextMatrix(point.Rect.Left, point.Rect.Top - 24);  //(xPos, yPos)
                            content.ShowText(DateTime.Now.ToString("yyyy-MMM-dd HH:mm:ss"));
                            content.EndText();


                        }
                        PdfSignatureAppearance appearance = stamper.SignatureAppearance;
                        IExternalSignature es = new PrivateKeySignature(kp.Private, "SHA-256");
                        MakeSignature.SignDetached(appearance, es, new X509Certificate[] { newCert }, null, null, null, 0, CryptoStandard.CMS);

                        stamper.Close();
                        reader.Close();
                    }
                }
                return outputPdfStream.ToArray();
            }
        }

        public static void AddWaterMark(PdfContentByte dc, string text, BaseFont font, float fontSize, float angle, BaseColor color, Rectangle realPageSize, Rectangle rect = null)
        {
            var gstate = new PdfGState { FillOpacity = 0.1f, StrokeOpacity = 0.3f };
            dc.SaveState();
            dc.SetGState(gstate);
            dc.SetColorFill(color);
            dc.BeginText();
            dc.SetFontAndSize(font, fontSize);
            var ps = rect ?? realPageSize; /*dc.PdfDocument.PageSize is not always correct*/
            var x = (ps.Right + ps.Left) / 2;
            var y = (ps.Bottom + ps.Top) / 2;
            dc.ShowTextAligned(iTextSharp.text.Element.ALIGN_CENTER, text, x, y, angle);
            dc.EndText();
            dc.RestoreState();
        }

        public static bool AreStringsSimilar(string string1, string string2)
        {
            // Check the strings for similarity using a simple algorithm
            // If the result is 1.0 (100%) they are the same, otherwise anything smaller means they are less similar.
            // We will allow anything less than .1 as this means the similarities are too small to be significant
            return (Functions.CompareStrings(string1, string2) > .1);
        }
