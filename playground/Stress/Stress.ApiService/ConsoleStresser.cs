// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

public static class ConsoleStresser
{
    public static void Stress()
    {
        // Required to write ANSI color codes to the console.
        var stdout = Console.OpenStandardOutput();
        var con = new StreamWriter(stdout, Encoding.ASCII);
        con.AutoFlush = true;
        Console.SetOut(con);

        Console.WriteLine();
        Console.WriteLine("Console stress");
        Console.WriteLine("==============");
        Console.WriteLine();

        Console.WriteLine("UTF-8 characters:");
        Console.WriteLine("Emojis: \U0001F600, \U0001F680, \U0001F4A9");
        Console.WriteLine("Chinese: \u8fd9\u662f\u4e00\u4e2a\u4e2d\u6587\u5b57\u7b26\u4e32"); // "This is a Chinese string"
        Console.WriteLine();

        Console.WriteLine("Entire URL in one segment:");
        Console.Write("\x1b[36mhttp://localhost:7000\x1b[0m");
        Console.WriteLine();

        Console.WriteLine("Scheme and host in separate segment to port:");
        Console.Write("\x1b[36mhttp://localhost\x1b[0m");
        Console.Write("\x1b[31m:7001\x1b[0m");
        Console.WriteLine();

        Console.WriteLine("Everything in different segments:");
        Console.Write("\x1b[36mhttp\x1b[0m");
        Console.Write("\x1b[31m://\x1b[0m");
        Console.Write("\x1b[32mlocalhost\x1b[0m");
        Console.Write("\x1b[33m:7002\x1b[0m");
        Console.WriteLine();
        Console.WriteLine();

        Console.WriteLine("URL examples:");
        Console.Write("\x1b[36m"); // Set color so we can see the difference between URLs and the rest of the text
        Console.WriteLine("https://www.example.com");
        Console.WriteLine("https://subdomain.example.com");
        Console.WriteLine("http://www.example.com");
        Console.WriteLine("http://example.com/path/to/page");
        Console.WriteLine("https://www.example.com?query=string&param=value");
        Console.WriteLine("https://www.example.com#anchor");
        Console.WriteLine("https://255.255.255.255");
        Console.WriteLine("ftp://example.com/resource");
        Console.WriteLine("mailto:user@example.com");
        Console.WriteLine("https://user:password@example.com");
        Console.WriteLine("https://example.com:8080");
        Console.WriteLine("https://example.com/path/to/page.html");
        Console.WriteLine("https://www.example.co.uk");
        Console.WriteLine("https://example.io");
        Console.WriteLine("https://xn--n3h.com (Punycode for Unicode characters)");
        Console.WriteLine("https://www.example.com/path%20with%20spaces");
        Console.WriteLine("https://[2001:db8::1] (IPv6 address)");
        Console.WriteLine("http://www.example.com/foo.php?bar[]=1&bar[]=2&bar[]=3");
        Console.WriteLine("https://www.example.com/path/to/page?#");
        Console.WriteLine("https://user:password@example.com/path?query=string#anchor");
        Console.WriteLine("https://www.example.com/? (Question mark at end of URL)");
        Console.WriteLine("https://example.com/# (Hash at end of URL)");
        Console.WriteLine("https://www.example.com/path/to/page/?query=string#anchor");
        Console.WriteLine("https://example.com./path/to/page/ (Dot at the end of domain)");
        Console.WriteLine("https://www-example.com (Dash in domain)");
        Console.WriteLine("https://example.com/path/to/page/index.html?query=string#section1");
        Console.WriteLine("https://example.com/empty/");
        Console.WriteLine("https://example.com/empty/. (Dot at end of URL)");
        Console.WriteLine("https://www.example.com/path/with/semicolon; (Semicolon at end of URL)");
        Console.WriteLine("https://www.example.com/path/with/semicolon, (Comma at end of URL)");
        Console.WriteLine("https://www.example.com/path/with/plus+sign");
        Console.WriteLine("https://www.example.com/path/with/equals=sign");
        Console.WriteLine("https://www.example.com/path/with/ampersand&sign");
        Console.WriteLine("https://www.example.com/path/with/percent%25encoded");
        Console.WriteLine("https://www.example.com/path/with/dollar$sign");
        Console.WriteLine("https://www.example.com/path/with/exclamation!mark");
        Console.WriteLine("https://www.example.com/;path/");
        Console.WriteLine("https://www.example.com/path/?query;string");
        Console.WriteLine("https://;www.example.com/");
        Console.WriteLine("https://www;.example.com/");
        Console.WriteLine("https://www.exa;mple.com/");

        Console.Write("\x1b[0m"); // reset color

        // write a script to display all ansi colors in the console
        Console.WriteLine();
        Console.WriteLine("ANSI Console Foreground and Background Colors");

        // Foreground Colors
        for (var color = 30; color <= 37; color++)
        {
            Console.Write("\x1b[" + color + "m"); // Set foreground color
            Console.WriteLine($"This is foreground color {color}");
        }
        Console.Write("\x1b[0m"); // Reset colors to default

        // Background Colors
        for (var color = 40; color <= 47; color++)
        {
            Console.Write("\x1b[" + color + "m"); // Set background color
            Console.Write($"This is background color {color}");
            Console.WriteLine("\x1b[0m"); // Reset colors to default after each background to maintain readability
        }
        Console.Write("\x1b[0m"); // Reset all colors to default at the end

        Console.WriteLine();
        Console.WriteLine("\u001b[36;45mThis text is Cyan with a Magenta background.\u001b[0m");
        Console.WriteLine("\u001b[31mThis text has a red foreground using ANSI escape codes.\u001b[0m");
        Console.WriteLine("\u001b[42mThis text has a green background using ANSI escape codes.\u001b[0m");
        Console.WriteLine("\u001b[1mThis text is bold using ANSI escape codes.\u001b[0m");
        Console.WriteLine("\u001b[4mThis text is underlined using ANSI escape codes.\u001b[0m");
        Console.WriteLine("\u001b[31;1;4mThis text is red, bold, and underlined.\u001b[0m");
        Console.WriteLine("\u001b[31;3;4mThis text is red, italic and underlined.\u001b[0m");
        Console.WriteLine("\u001b[31;3;4;9mThis text is red, italic and strikethrough.\u001b[0m");
        Console.WriteLine("\u001b[31;42;3;4mThis text is red, green background, italic and underlined.\u001b[0m");

        Console.WriteLine();
        Console.WriteLine("\u001b[38;5;221mThis text is a Xterm text color using ANSI escape codes.\u001b[0m");
        Console.WriteLine("\u001b[48;5;95mThis text is a Xterm bakground color using ANSI escape codes.\u001b[0m");
        Console.WriteLine("\u001b[38;5;221m\u001b[48;5;95mThis text is a Xterm text and bakground color using ANSI escape codes.\u001b[0m");
        Console.WriteLine("\u001b[38;5;243mThis text is a Xterm gray text color using ANSI escape codes.\u001b[0m");
        Console.WriteLine("\u001b[48;5;243mThis text is a Xterm gray background color using ANSI escape codes.\u001b[0m");
        Console.WriteLine("\u001b[38;5;1mThis text is a Xterm red color using ANSI escape codes.\u001b[0m");
        Console.WriteLine("\u001b[38;5;9mThis text is a Xterm bright red color using ANSI escape codes.\u001b[0m");
        Console.WriteLine("\u001b[48;5;1mThis text is a Xterm red background color using ANSI escape codes.\u001b[0m");
        Console.WriteLine("\u001b[48;5;9mThis text is a Xterm bright red background color using ANSI escape codes.\u001b[0m");
        Console.WriteLine("\u001b[3;38;5;9mThis text is a Xterm bright red color and italic using ANSI escape codes.\u001b[0m");
        Console.WriteLine("\u001b[4;38;5;9mThis text is a Xterm bright red color and underlined using ANSI escape codes.\u001b[0m");
        Console.WriteLine("\u001b[9;38;5;9mThis text is a Xterm bright red color and strikethrough using ANSI escape codes.\u001b[0m");

        Console.WriteLine();
        Console.WriteLine("\u001b[38;5;321mThis text is a Xterm text color using invalid color value.\u001b[0m");
        Console.WriteLine("\u001b[38;5;100This text is a Xterm text color using unfinished escape sequence (m here to finish the sequence late).\u001b[0m");
        Console.WriteLine("\u001b[38;5;100This text is a Xterm text color using unfinished escape sequence.\u001b[0m");

        Console.WriteLine();
        Console.WriteLine("A link in escape sequence: \u001b]8;;https://example.com\u001b\\The link text\u001b]8;;\u001b\\");
        Console.WriteLine("A link with formatted link text in escape sequnce: \u001b]8;;https://example.com\u001b\\The \u001b[38;5;100mlink\u001b[0m formatted text\u001b]8;;\u001b\\");

        Console.WriteLine();
        Console.WriteLine("HTML:");
        Console.WriteLine("<b>BOLD!</b>");
        Console.WriteLine("<script>alert('Hello, World!')</script>");
        Console.WriteLine("<span style=\"color: #ff0000; background-color: #00ff00; font-weight: bold; text-decoration: underline;\">This text is red with a green background, bold, and underlined using HTML.</span>");

        Console.WriteLine();
        Console.WriteLine("Long continious !!! content:");
        Console.WriteLine(new string('!', 1000));
        Console.WriteLine();
        Console.WriteLine("Long continious letter content:");
        for (int i = 0; i < 50; i++)
        {
            Console.Write(new string(i % 2 == 0 ? 'a' : 'b', 50));
        }
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("Long world content:");
        for (int i = 0; i < 50; i++)
        {
            if (i > 0)
            {
                Console.Write(" ");
            }
            Console.Write(new string(i % 2 == 0 ? 'a' : 'b', 50));
        }
        Console.WriteLine();
    }
}
