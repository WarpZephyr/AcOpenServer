## OpenSslNet

### Description

A managed [OpenSSL](https://www.openssl.org/) wrapper written in C# for .NET 8.0 that exposes both the [Crypto API](https://www.openssl.org/docs/crypto/crypto.html) and the [SSL API](https://www.openssl.org/docs/ssl/ssl.html).

This modified wrapper is using openssl version 1.1.1n.

### Wrapper Example

The following is a partial example to show the general pattern of wrapping onto the C API.

Take DSA and the following C prototypes:

```
DSA *  DSA_new(void);
void   DSA_free(DSA *dsa);
int    DSA_size(const DSA *dsa);
int    DSA_generate_key(DSA *dsa);
int    DSA_sign(int dummy, const unsigned char *dgst, int len,
                unsigned char *sigret, unsigned int *siglen, DSA *dsa);
int    DSA_verify(int dummy, const unsigned char *dgst, int len,
                const unsigned char *sigbuf, int siglen, DSA *dsa);
```

Which gets wrapped as something akin to:

```
public class DSA : IDisposable
{
    // calls DSA_new()
    public DSA();

    // calls DSA_free() as needed
    ~DSA();

    // calls DSA_free() as needed
    public void Dispose();

    // returns DSA_size()
    public int Size { get; }

    // calls DSA_generate_key()
    public void GenerateKeys();

    // calls DSA_sign()
    public byte[] Sign(byte[] msg);

    // returns DSA_verify()
    public bool Verify(byte[] msg, byte[] sig);
}
```

### Installation

Make sure you have `libcrypto-1_1-x64.dll` and `libssl-1_1-x64.dll` in the current working directory of your application or in your `PATH`.  
In your .NET project, add a reference to the `OpenSslNet.dll` assembly.

### Documentation

Take a look at the low-level C API [documentation](https://www.openssl.org/docs).

* [Changes](CHANGES)

### License

The native OpenSSL libraries are distributed under the terms of the [OpenSSL License & SSLeay License](LICENSE).  
The original openssl-net library and related code are released under the BSD license, see [COPYING](COPYING) for more details.