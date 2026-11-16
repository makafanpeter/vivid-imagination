namespace VividImagination.Shared.Infrastructure.Configurations;

public class PaymentSettings
{
    /// <summary>
    /// Represents the configuration settings specific to the POS (Point of Sale) channel.
    /// </summary>
    /// <remarks>
    /// This property is used to define and manage the specific settings required
    /// for POS transactions within the payment processing system.
    /// </remarks>
    public ChannelSettings POS { get; set; } = new();

    /// <summary>
    /// Represents the configuration settings specific to the E-Commerce channel.
    /// </summary>
    /// <remarks>
    /// This property is utilized to configure and manage the settings required
    /// for processing E-Commerce transactions within the payment system.
    /// </remarks>
    public ChannelSettings ECommerce { get; set; } = new();

    /// <summary>
    /// Represents the configuration settings specific to the QRCode payment channel.
    /// </summary>
    /// <remarks>
    /// This property is used to define and manage the settings required
    /// for processing payments through the QRCode payment method.
    /// </remarks>
    public ChannelSettings QRCode { get; set; } = new();

    /// <summary>
    /// Represents the configuration settings specific to the Mobile POS (Point of Sale) channel.
    /// </summary>
    /// <remarks>
    /// This property is intended to manage the operational parameters and settings
    /// required for enabling and handling transactions through the Mobile POS channel.
    /// </remarks>
    public ChannelSettings MobilePOS { get; set; } = new();

    /// <summary>
    /// Represents the configuration settings specific to the MOTO (Mail Order/Telephone Order) channel.
    /// </summary>
    /// <remarks>
    /// This property is used to configure and manage the settings required for processing
    /// payments made through Mail Order or Telephone Order methods.
    /// </remarks>
    public ChannelSettings MOTO { get; set; } = new();

    /// <summary>
    /// Represents the configuration settings specific to the Payment Link channel.
    /// </summary>
    /// <remarks>
    /// This property is used to define and manage the settings required
    /// for processing transactions through Payment Link functionality.
    /// </remarks>
    public ChannelSettings PaymentLink { get; set; } = new();

    /// <summary>
    /// Represents the configuration settings specific to recurring payments.
    /// </summary>
    /// <remarks>
    /// This property is utilized to manage and define the operational parameters
    /// for handling recurring transactions within the payment processing system.
    /// </remarks>
    public ChannelSettings Recurring { get; set; } = new();

    /// <summary>
    /// Represents the configuration settings specific to the In-App channel.
    /// </summary>
    /// <remarks>
    /// This property is used to define and manage the specific settings required
    /// for payment transactions within mobile or in-app environments.
    /// </remarks>
    public ChannelSettings InApp { get; set; } = new();

    /// <summary>
    /// Represents the configuration settings specific to the Virtual Terminal channel.
    /// </summary>
    /// <remarks>
    /// This property is used to define and manage the specific settings required
    /// for Virtual Terminal transactions within the payment processing system.
    /// </remarks>
    public ChannelSettings VirtualTerminal { get; set; } = new();

    /// <summary>
    /// Represents the configuration settings specific to the Kiosk channel.
    /// </summary>
    /// <remarks>
    /// This property is utilized to configure and manage the settings required
    /// for transactions conducted through kiosk-based payment systems.
    /// </remarks>
    public ChannelSettings Kiosk { get; set; } = new();

    /// <summary>
    /// Represents the configuration settings specific to the API Integration channel.
    /// </summary>
    /// <remarks>
    /// This property is used to define and manage the settings required for processing
    /// payments through API-based integrations within the payment system.
    /// </remarks>
    public ChannelSettings APIIntegration { get; set; } = new();
    
}