import Foundation
import UIKit
import ImageIO
import AVFoundation
import CoreLocation
import Display
import Postbox
import TelegramCore
import AccountContext
import MediaResources

private struct AorusMetadataRow {
    let title: String
    let value: String
}

private struct AorusMetadataSection {
    let title: String
    var rows: [AorusMetadataRow]
}

private struct AorusMetadataResult {
    var sections: [AorusMetadataSection]
    var coordinate: CLLocationCoordinate2D?
}

public enum AorusMediaMetadata {
    public static let enabledKey = "aorusgram_media_metadata_enabled"

    public static func hasSupportedMedia(_ message: Message) -> Bool {
        return firstSupportedMedia(in: message) != nil
    }

    public static func present(context: AccountContext, message: Message) {
        let isRu = AorusLang.current == .ru
        let controller = AorusMediaMetadataViewController(isRu: isRu)
        let navigation = UINavigationController(rootViewController: controller)
        navigation.modalPresentationStyle = .pageSheet
        topController()?.present(navigation, animated: true)

        DispatchQueue.global(qos: .userInitiated).async {
            let result = collect(context: context, message: message, isRu: isRu)
            DispatchQueue.main.async {
                controller.update(result.sections)
                if let coordinate = result.coordinate {
                    reverseGeocode(coordinate: coordinate, isRu: isRu) { rows in
                        controller.replaceLocationRows(rows)
                    }
                }
            }
        }
    }

    private static func topController() -> UIViewController? {
        var window: UIWindow?
        for scene in UIApplication.shared.connectedScenes {
            guard let windowScene = scene as? UIWindowScene else { continue }
            window = windowScene.windows.first(where: { $0.isKeyWindow }) ?? windowScene.windows.first
            if window != nil { break }
        }
        var controller = window?.rootViewController
        while let presented = controller?.presentedViewController {
            controller = presented
        }
        return controller
    }

    private enum SupportedMedia {
        case image(TelegramMediaImage)
        case file(TelegramMediaFile)
    }

    private static func firstSupportedMedia(in message: Message) -> SupportedMedia? {
        for media in message.media {
            if let image = media as? TelegramMediaImage {
                return .image(image)
            }
            if let file = media as? TelegramMediaFile {
                if file.isSticker || file.isAnimatedSticker || file.isVideoSticker {
                    continue
                }
                if file.isVideo || file.isAnimated {
                    return .file(file)
                }
            }
        }
        return nil
    }

    private static func collect(context: AccountContext, message: Message, isRu: Bool) -> AorusMetadataResult {
        guard let media = firstSupportedMedia(in: message) else {
            return AorusMetadataResult(sections: [AorusMetadataSection(title: title("Файл", "File", isRu), rows: [
                AorusMetadataRow(title: title("Статус", "Status", isRu), value: title("Поддерживаемых метаданных нет", "No supported metadata", isRu))
            ])], coordinate: nil)
        }

        var sections: [AorusMetadataSection] = []
        var coordinate: CLLocationCoordinate2D?
        let mediaBox = context.account.postbox.mediaBox

        switch media {
        case let .image(image):
            var rows: [AorusMetadataRow] = [
                AorusMetadataRow(title: title("Тип", "Type", isRu), value: title("Фото", "Photo", isRu))
            ]
            if let representation = largestImageRepresentation(image.representations) {
                rows.append(AorusMetadataRow(title: title("Разрешение", "Resolution", isRu), value: "\(representation.dimensions.width) x \(representation.dimensions.height)"))
                if let path = bestImageMetadataPath(image: image, mediaBox: mediaBox, isRu: isRu) ?? mediaBox.completedResourcePath(representation.resource) {
                    appendFileRows(path: path, rows: &rows, isRu: isRu)
                    let parsed = parseImageMetadata(path: path, isRu: isRu)
                    sections.append(AorusMetadataSection(title: title("Основное", "Summary", isRu), rows: rows))
                    sections.append(contentsOf: parsed.sections)
                    coordinate = parsed.coordinate
                } else {
                    rows.append(AorusMetadataRow(title: title("Файл", "File", isRu), value: title("ещё не загружен", "not downloaded yet", isRu)))
                    sections.append(AorusMetadataSection(title: title("Основное", "Summary", isRu), rows: rows))
                }
            } else {
                sections.append(AorusMetadataSection(title: title("Основное", "Summary", isRu), rows: rows))
            }
        case let .file(file):
            var rows: [AorusMetadataRow] = [
                AorusMetadataRow(title: title("Тип", "Type", isRu), value: file.isAnimated ? "GIF" : title("Видео", "Video", isRu)),
                AorusMetadataRow(title: title("MIME", "MIME", isRu), value: file.mimeType)
            ]
            if let name = file.fileName, !name.isEmpty {
                rows.append(AorusMetadataRow(title: title("Имя файла", "File Name", isRu), value: name))
            }
            if let size = file.size {
                rows.append(AorusMetadataRow(title: title("Размер", "Size", isRu), value: ByteCountFormatter.string(fromByteCount: size, countStyle: .file)))
            }
            for attribute in file.attributes {
                if case let .Video(duration, dimensions, _, _, _, _) = attribute {
                    rows.append(AorusMetadataRow(title: title("Длительность", "Duration", isRu), value: durationText(duration)))
                    rows.append(AorusMetadataRow(title: title("Разрешение", "Resolution", isRu), value: "\(dimensions.width) x \(dimensions.height)"))
                }
            }
            if let path = completedPath(for: file, mediaBox: mediaBox) {
                appendFileRows(path: path, rows: &rows, isRu: isRu)
                sections.append(AorusMetadataSection(title: title("Основное", "Summary", isRu), rows: rows))
                sections.append(contentsOf: parseVideoMetadata(path: path, isRu: isRu))
                let parsedImage = parseImageMetadata(path: path, isRu: isRu)
                if !parsedImage.sections.isEmpty {
                    sections.append(contentsOf: parsedImage.sections)
                    coordinate = parsedImage.coordinate
                }
            } else {
                rows.append(AorusMetadataRow(title: title("Файл", "File", isRu), value: title("ещё не загружен", "not downloaded yet", isRu)))
                sections.append(AorusMetadataSection(title: title("Основное", "Summary", isRu), rows: rows))
            }
        }

        if sections.isEmpty {
            sections.append(AorusMetadataSection(title: title("Метаданные", "Metadata", isRu), rows: [
                AorusMetadataRow(title: title("Статус", "Status", isRu), value: title("данных нет", "no data", isRu))
            ]))
        }
        return AorusMetadataResult(sections: sections, coordinate: coordinate)
    }

    private static func bestImageMetadataPath(image: TelegramMediaImage, mediaBox: MediaBox, isRu: Bool) -> String? {
        var best: (path: String, score: Int)?
        for representation in image.representations {
            guard let path = mediaBox.completedResourcePath(representation.resource) else {
                continue
            }
            let parsed = parseImageMetadata(path: path, isRu: isRu)
            let score = parsed.sections.reduce(0) { $0 + $1.rows.count } + (parsed.coordinate == nil ? 0 : 1000)
            if best == nil || score > best!.score {
                best = (path, score)
            }
        }
        return best?.path
    }

    private static func completedPath(for file: TelegramMediaFile, mediaBox: MediaBox) -> String? {
        if let path = mediaBox.completedResourcePath(file.resource) {
            return path
        }
        if let ext = file.fileName?.split(separator: ".").last, !ext.isEmpty {
            return mediaBox.completedResourcePath(file.resource, pathExtension: String(ext))
        }
        if file.mimeType == "video/mp4" {
            return mediaBox.completedResourcePath(file.resource, pathExtension: "mp4")
        }
        if file.mimeType == "image/gif" {
            return mediaBox.completedResourcePath(file.resource, pathExtension: "gif")
        }
        return nil
    }

    private static func appendFileRows(path: String, rows: inout [AorusMetadataRow], isRu: Bool) {
        if let attrs = try? FileManager.default.attributesOfItem(atPath: path) {
            if let size = attrs[.size] as? NSNumber {
                rows.append(AorusMetadataRow(title: title("Размер на диске", "File Size", isRu), value: ByteCountFormatter.string(fromByteCount: size.int64Value, countStyle: .file)))
            }
            if let created = attrs[.creationDate] as? Date {
                rows.append(AorusMetadataRow(title: title("Создан", "Created", isRu), value: dateText(created)))
            }
            if let modified = attrs[.modificationDate] as? Date {
                rows.append(AorusMetadataRow(title: title("Изменён", "Modified", isRu), value: dateText(modified)))
            }
        }
    }

    private static func parseImageMetadata(path: String, isRu: Bool) -> AorusMetadataResult {
        guard let source = CGImageSourceCreateWithURL(URL(fileURLWithPath: path) as CFURL, nil),
              let properties = CGImageSourceCopyPropertiesAtIndex(source, 0, nil) as? [String: Any] else {
            return AorusMetadataResult(sections: [], coordinate: nil)
        }

        var sections: [AorusMetadataSection] = []
        var technical: [AorusMetadataRow] = []
        add(properties[kCGImagePropertyPixelWidth as String], title("Ширина", "Width", isRu), to: &technical)
        add(properties[kCGImagePropertyPixelHeight as String], title("Высота", "Height", isRu), to: &technical)
        add(properties[kCGImagePropertyColorModel as String], title("Цвет", "Color", isRu), to: &technical)
        add(properties[kCGImagePropertyDepth as String], title("Глубина цвета", "Color Depth", isRu), to: &technical)
        add(properties[kCGImagePropertyDPIWidth as String], title("DPI X", "DPI X", isRu), to: &technical)
        add(properties[kCGImagePropertyDPIHeight as String], title("DPI Y", "DPI Y", isRu), to: &technical)
        if !technical.isEmpty {
            sections.append(AorusMetadataSection(title: title("Изображение", "Image", isRu), rows: technical))
        }

        var cameraRows: [AorusMetadataRow] = []
        if let tiff = properties[kCGImagePropertyTIFFDictionary as String] as? [String: Any] {
            let rows = flattenedRows(from: tiff, isRu: isRu)
            cameraRows.append(contentsOf: rows)
        }
        if !cameraRows.isEmpty {
            sections.append(AorusMetadataSection(title: title("Камера", "Camera", isRu), rows: cameraRows))
        } else {
            sections.append(AorusMetadataSection(title: title("Камера", "Camera", isRu), rows: [
                AorusMetadataRow(title: title("Данные камеры", "Camera Data", isRu), value: title("нет данных", "no data", isRu))
            ]))
        }

        if let exif = properties[kCGImagePropertyExifDictionary as String] as? [String: Any] {
            let rows = flattenedRows(from: exif, isRu: isRu)
            if !rows.isEmpty {
                sections.append(AorusMetadataSection(title: "EXIF", rows: rows))
            } else {
                sections.append(AorusMetadataSection(title: "EXIF", rows: [
                    AorusMetadataRow(title: title("EXIF", "EXIF", isRu), value: title("нет данных", "no data", isRu))
                ]))
            }
        } else {
            sections.append(AorusMetadataSection(title: "EXIF", rows: [
                AorusMetadataRow(title: title("EXIF", "EXIF", isRu), value: title("нет данных", "no data", isRu))
            ]))
        }

        var coordinate: CLLocationCoordinate2D?
        if let gps = properties[kCGImagePropertyGPSDictionary as String] as? [String: Any] {
            let parsed = gpsRows(from: gps, isRu: isRu)
            sections.append(AorusMetadataSection(title: "GPS", rows: parsed.rows.isEmpty ? noGPSRows(isRu: isRu) : parsed.rows))
            coordinate = parsed.coordinate
        } else {
            sections.append(AorusMetadataSection(title: "GPS", rows: noGPSRows(isRu: isRu)))
        }

        let allRows = flattenedRows(from: properties, isRu: isRu)
        if !allRows.isEmpty {
            sections.append(AorusMetadataSection(title: title("Все поля", "All Fields", isRu), rows: allRows))
        }

        return AorusMetadataResult(sections: sections, coordinate: coordinate)
    }

    private static func parseVideoMetadata(path: String, isRu: Bool) -> [AorusMetadataSection] {
        let asset = AVURLAsset(url: URL(fileURLWithPath: path))
        var sections: [AorusMetadataSection] = []
        var rows: [AorusMetadataRow] = [
            AorusMetadataRow(title: title("Длительность", "Duration", isRu), value: durationText(CMTimeGetSeconds(asset.duration)))
        ]

        for track in asset.tracks {
            if track.mediaType == .video {
                let size = track.naturalSize.applying(track.preferredTransform)
                rows.append(AorusMetadataRow(title: title("Видео", "Video", isRu), value: "\(Int(abs(size.width))) x \(Int(abs(size.height)))"))
                if track.nominalFrameRate > 0 {
                    rows.append(AorusMetadataRow(title: "FPS", value: String(format: "%.2f", track.nominalFrameRate)))
                }
                if track.estimatedDataRate > 0 {
                    rows.append(AorusMetadataRow(title: title("Битрейт", "Bitrate", isRu), value: ByteCountFormatter.string(fromByteCount: Int64(track.estimatedDataRate / 8.0), countStyle: .file) + "/s"))
                }
            } else if track.mediaType == .audio {
                rows.append(AorusMetadataRow(title: title("Аудио", "Audio", isRu), value: title("есть", "present", isRu)))
            }
        }
        sections.append(AorusMetadataSection(title: title("Видео", "Video", isRu), rows: rows))

        let common = asset.commonMetadata.compactMap { item -> AorusMetadataRow? in
            guard let value = item.value else { return nil }
            let key = item.commonKey?.rawValue ?? item.identifier?.rawValue ?? title("Поле", "Field", isRu)
            return AorusMetadataRow(title: friendlyKey(key, isRu: isRu), value: cleanValue(value))
        }
        if !common.isEmpty {
            sections.append(AorusMetadataSection(title: title("Контейнер", "Container", isRu), rows: common))
        }
        return sections
    }

    private static func gpsRows(from gps: [String: Any], isRu: Bool) -> (rows: [AorusMetadataRow], coordinate: CLLocationCoordinate2D?) {
        var rows: [AorusMetadataRow] = []
        var lat = number(gps[kCGImagePropertyGPSLatitude as String])
        var lon = number(gps[kCGImagePropertyGPSLongitude as String])
        if let ref = gps[kCGImagePropertyGPSLatitudeRef as String] as? String, ref.uppercased() == "S", let value = lat {
            lat = -value
        }
        if let ref = gps[kCGImagePropertyGPSLongitudeRef as String] as? String, ref.uppercased() == "W", let value = lon {
            lon = -value
        }
        if let lat = lat, let lon = lon {
            rows.append(AorusMetadataRow(title: title("Координаты", "Coordinates", isRu), value: String(format: "%.6f, %.6f", lat, lon)))
            rows.append(AorusMetadataRow(title: title("Город", "City", isRu), value: title("определяется...", "detecting...", isRu)))
        }
        add(gps[kCGImagePropertyGPSAltitude as String], title("Высота", "Altitude", isRu), to: &rows)
        add(gps[kCGImagePropertyGPSSpeed as String], title("Скорость", "Speed", isRu), to: &rows)
        add(gps[kCGImagePropertyGPSImgDirection as String], title("Направление", "Direction", isRu), to: &rows)
        add(gps[kCGImagePropertyGPSTimeStamp as String], title("Время GPS", "GPS Time", isRu), to: &rows)
        let coordinate = (lat != nil && lon != nil) ? CLLocationCoordinate2D(latitude: lat!, longitude: lon!) : nil
        return (rows, coordinate)
    }

    private static func reverseGeocode(coordinate: CLLocationCoordinate2D, isRu: Bool, completion: @escaping ([AorusMetadataRow]) -> Void) {
        let location = CLLocation(latitude: coordinate.latitude, longitude: coordinate.longitude)
        CLGeocoder().reverseGeocodeLocation(location) { placemarks, _ in
            var rows: [AorusMetadataRow] = [
                AorusMetadataRow(title: title("Координаты", "Coordinates", isRu), value: String(format: "%.6f, %.6f", coordinate.latitude, coordinate.longitude))
            ]
            if let place = placemarks?.first {
                let city = place.locality ?? place.subAdministrativeArea ?? place.administrativeArea
                if let city = city, !city.isEmpty {
                    rows.append(AorusMetadataRow(title: title("Город", "City", isRu), value: city))
                }
                if let country = place.country, !country.isEmpty {
                    rows.append(AorusMetadataRow(title: title("Страна", "Country", isRu), value: country))
                }
                if let name = place.name, !name.isEmpty, name != city {
                    rows.append(AorusMetadataRow(title: title("Место", "Place", isRu), value: name))
                }
            } else {
                rows.append(AorusMetadataRow(title: title("Город", "City", isRu), value: title("не найден", "not found", isRu)))
            }
            completion(rows)
        }
    }

    private static func noGPSRows(isRu: Bool) -> [AorusMetadataRow] {
        return [
            AorusMetadataRow(title: title("Координаты", "Coordinates", isRu), value: title("нет данных", "no data", isRu)),
            AorusMetadataRow(title: title("Город", "City", isRu), value: title("нет данных", "no data", isRu))
        ]
    }

    private static func flattenedRows(from dict: [String: Any], prefix: String? = nil, isRu: Bool) -> [AorusMetadataRow] {
        var rows: [AorusMetadataRow] = []
        for key in dict.keys.sorted() {
            guard let value = dict[key] else { continue }
            let label = prefix.map { "\($0) / \(friendlyKey(key, isRu: isRu))" } ?? friendlyKey(key, isRu: isRu)
            if let nested = value as? [String: Any] {
                rows.append(contentsOf: flattenedRows(from: nested, prefix: label, isRu: isRu))
            } else if let dictionary = value as? NSDictionary {
                var nested: [String: Any] = [:]
                for (rawKey, rawValue) in dictionary {
                    if let rawKey = rawKey as? String {
                        nested[rawKey] = rawValue
                    }
                }
                if !nested.isEmpty {
                    rows.append(contentsOf: flattenedRows(from: nested, prefix: label, isRu: isRu))
                }
            } else {
                let text = cleanValue(value)
                if !text.isEmpty {
                    rows.append(AorusMetadataRow(title: label, value: text))
                }
            }
        }
        return rows
    }

    private static func add(_ value: Any?, _ name: String, to rows: inout [AorusMetadataRow]) {
        guard let value = value else { return }
        let text = cleanValue(value)
        if !text.isEmpty {
            rows.append(AorusMetadataRow(title: name, value: text))
        }
    }

    private static func number(_ value: Any?) -> Double? {
        if let n = value as? NSNumber { return n.doubleValue }
        if let s = value as? String { return Double(s) }
        return nil
    }

    private static func cleanValue(_ value: Any) -> String {
        if let date = value as? Date {
            return dateText(date)
        }
        if let number = value as? NSNumber {
            return number.stringValue
        }
        return "\(value)"
    }

    private static func friendlyKey(_ key: String, isRu: Bool) -> String {
        let cleaned = key
            .replacingOccurrences(of: "{Exif}", with: "")
            .replacingOccurrences(of: "{TIFF}", with: "")
            .replacingOccurrences(of: "{GPS}", with: "")
            .replacingOccurrences(of: "{IPTC}", with: "")
            .replacingOccurrences(of: "{PNG}", with: "")
            .replacingOccurrences(of: "{JFIF}", with: "")
            .replacingOccurrences(of: "kCGImageProperty", with: "")
        let map: [String: String] = [
            "Make": title("Производитель", "Make", isRu),
            "Model": title("Модель", "Model", isRu),
            "Software": title("ПО", "Software", isRu),
            "DateTime": title("Дата", "Date", isRu),
            "DateTimeOriginal": title("Дата съёмки", "Original Date", isRu),
            "LensModel": title("Объектив", "Lens", isRu),
            "ISOSpeedRatings": "ISO",
            "ExposureTime": title("Выдержка", "Exposure", isRu),
            "FNumber": title("Диафрагма", "Aperture", isRu),
            "FocalLength": title("Фокусное расстояние", "Focal Length", isRu),
            "DigitalZoomRatio": title("Цифровой зум", "Digital Zoom", isRu),
            "LensMake": title("Производитель объектива", "Lens Make", isRu),
            "LensSpecification": title("Параметры объектива", "Lens Specification", isRu),
            "Flash": title("Вспышка", "Flash", isRu),
            "Orientation": title("Ориентация", "Orientation", isRu),
            "PixelWidth": title("Ширина", "Width", isRu),
            "PixelHeight": title("Высота", "Height", isRu),
            "ColorModel": title("Цвет", "Color", isRu),
            "Depth": title("Глубина цвета", "Color Depth", isRu),
            "ProfileName": title("Цветовой профиль", "Color Profile", isRu),
            "Latitude": title("Широта", "Latitude", isRu),
            "Longitude": title("Долгота", "Longitude", isRu),
            "Altitude": title("Высота", "Altitude", isRu),
            "Speed": title("Скорость", "Speed", isRu),
            "ImgDirection": title("Направление", "Direction", isRu),
            "title": title("Название", "Title", isRu),
            "artist": title("Автор", "Artist", isRu),
            "creationDate": title("Дата создания", "Creation Date", isRu),
            "description": title("Описание", "Description", isRu)
        ]
        return map[cleaned] ?? cleaned
    }

    private static func durationText(_ seconds: Double) -> String {
        guard seconds.isFinite, seconds > 0 else { return "0:00" }
        let total = Int(seconds.rounded())
        let h = total / 3600
        let m = (total % 3600) / 60
        let s = total % 60
        if h > 0 {
            return String(format: "%d:%02d:%02d", h, m, s)
        }
        return String(format: "%d:%02d", m, s)
    }

    private static func dateText(_ date: Date) -> String {
        let formatter = DateFormatter()
        formatter.dateStyle = .medium
        formatter.timeStyle = .medium
        return formatter.string(from: date)
    }

    private static func title(_ ru: String, _ en: String, _ isRu: Bool) -> String {
        return isRu ? ru : en
    }
}

private final class AorusMediaMetadataViewController: UIViewController, UITableViewDataSource {
    private let tableView = UITableView(frame: .zero, style: .insetGrouped)
    private let isRu: Bool
    private var sections: [AorusMetadataSection] = []

    init(isRu: Bool) {
        self.isRu = isRu
        super.init(nibName: nil, bundle: nil)
        self.title = isRu ? "Метаданные" : "Metadata"
    }

    required init?(coder: NSCoder) {
        fatalError("init(coder:) has not been implemented")
    }

    override func viewDidLoad() {
        super.viewDidLoad()
        self.view.backgroundColor = .systemGroupedBackground
        self.navigationItem.leftBarButtonItem = UIBarButtonItem(title: isRu ? "Готово" : "Done", style: .done, target: self, action: #selector(close))
        self.navigationItem.rightBarButtonItem = UIBarButtonItem(title: isRu ? "Копировать" : "Copy", style: .plain, target: self, action: #selector(copyAll))
        self.tableView.dataSource = self
        self.tableView.autoresizingMask = [.flexibleWidth, .flexibleHeight]
        self.tableView.frame = self.view.bounds
        self.view.addSubview(self.tableView)
        self.sections = [AorusMetadataSection(title: "", rows: [
            AorusMetadataRow(title: isRu ? "Статус" : "Status", value: isRu ? "чтение..." : "reading...")
        ])]
    }

    func update(_ sections: [AorusMetadataSection]) {
        self.sections = sections
        self.tableView.reloadData()
    }

    func replaceLocationRows(_ rows: [AorusMetadataRow]) {
        guard let index = self.sections.firstIndex(where: { $0.title == "GPS" }) else { return }
        var section = self.sections[index]
        let otherRows = section.rows.filter { $0.title != (isRu ? "Координаты" : "Coordinates") && $0.title != (isRu ? "Город" : "City") }
        section.rows = rows + otherRows
        self.sections[index] = section
        self.tableView.reloadSections(IndexSet(integer: index), with: .automatic)
    }

    func numberOfSections(in tableView: UITableView) -> Int {
        return self.sections.count
    }

    func tableView(_ tableView: UITableView, numberOfRowsInSection section: Int) -> Int {
        return self.sections[section].rows.count
    }

    func tableView(_ tableView: UITableView, titleForHeaderInSection section: Int) -> String? {
        let title = self.sections[section].title
        return title.isEmpty ? nil : title
    }

    func tableView(_ tableView: UITableView, cellForRowAt indexPath: IndexPath) -> UITableViewCell {
        let row = self.sections[indexPath.section].rows[indexPath.row]
        let cell = UITableViewCell(style: .subtitle, reuseIdentifier: nil)
        cell.selectionStyle = .none
        cell.textLabel?.text = row.title
        cell.textLabel?.font = Font.with(size: 15.0, weight: .semibold)
        cell.detailTextLabel?.text = row.value
        cell.detailTextLabel?.font = Font.with(size: 13.0, weight: .regular)
        cell.detailTextLabel?.numberOfLines = 0
        return cell
    }

    @objc private func close() {
        self.dismiss(animated: true)
    }

    @objc private func copyAll() {
        var lines: [String] = []
        for section in sections {
            if !section.title.isEmpty {
                lines.append(section.title)
            }
            for row in section.rows {
                lines.append("\(row.title): \(row.value)")
            }
            lines.append("")
        }
        UIPasteboard.general.string = lines.joined(separator: "\n")
    }
}
